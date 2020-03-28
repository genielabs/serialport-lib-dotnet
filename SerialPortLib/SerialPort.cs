/*
  This file is part of SerialPortLib (https://github.com/genielabs/serialport-lib-dotnet)
 
  Copyright (2012-2018) G-Labs (https://github.com/genielabs)

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://github.com/genielabs/serialport-lib-dotnet
 */

using System;
using System.Threading;

#if NET40 || NET461
using NLog;
using System.IO.Ports;
#endif

#if NETSTANDARD2_0
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using RJCP.IO.Ports;
#endif

namespace SerialPortLib
{

    /// <summary>
    /// DataBits enum.
    /// </summary>
    public enum DataBits
    {
        /// <summary>
        /// DataBits 5.
        /// </summary>
        Five = 5,
        /// <summary>
        /// DataBits 6.
        /// </summary>
        Six,
        /// <summary>
        /// DataBits 7.
        /// </summary>
        Seven,
        /// <summary>
        /// DataBits 8.
        /// </summary>
        Eight,
    }

    /// <summary>
    /// Serial port I/O
    /// </summary>
    public class SerialPortInput
    {

        #region Private Fields

#if NET40 || NET461
        internal static Logger logger = LogManager.GetCurrentClassLogger();
#else
        private readonly ILogger<SerialPortInput> _logger;
#endif
#if NETSTANDARD2_0
        private SerialPortStream _serialPort;
#else
        private SerialPort _serialPort;
#endif

        private string _portName = "";
        private int _baudRate = 115200;
        private StopBits _stopBits = StopBits.One;
        private Parity _parity = Parity.None;
        private DataBits _dataBits = DataBits.Eight;

        // Read/Write error state variable
        private bool gotReadWriteError = true;

        // Serial port reader task
        private Thread reader;
        private CancellationTokenSource readerCts;
        // Serial port connection watcher
        private Thread connectionWatcher;
        private CancellationTokenSource connectionWatcherCts;

        private object accessLock = new object();
        private bool disconnectRequested = false;

        #endregion

        #region Public Events

        /// <summary>
        /// Connected state changed event.
        /// </summary>
        public delegate void ConnectionStatusChangedEventHandler(object sender, ConnectionStatusChangedEventArgs args);

        /// <summary>
        /// Occurs when connected state changed.
        /// </summary>
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        /// <summary>
        /// Message received event.
        /// </summary>
        public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);

        /// <summary>
        /// Occurs when message received.
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        #endregion

        #region Public Members

#if NET40 || NET461
        public SerialPortInput()
        {
            connectionWatcherCts = new CancellationTokenSource();
            readerCts = new CancellationTokenSource();
        }
#endif

#if NETSTANDARD2_0
        public SerialPortInput(ILogger<SerialPortInput> logger)
        {
            _logger = logger;
            connectionWatcherCts = new CancellationTokenSource();
            readerCts = new CancellationTokenSource();
        }
#endif

        /// <summary>
        /// Connect to the serial port.
        /// </summary>
        public bool Connect()
        {
            if (disconnectRequested)
                return false;
            lock (accessLock)
            {
                Disconnect();
                Open();
                connectionWatcherCts = new CancellationTokenSource();
                connectionWatcher = new Thread(ConnectionWatcherTask);
                connectionWatcher.Start(connectionWatcherCts.Token);
            }
            return IsConnected;
        }

        /// <summary>
        /// Disconnect the serial port.
        /// </summary>
        public void Disconnect()
        {
            if (disconnectRequested)
                return;
            disconnectRequested = true;
            Close();
            lock (accessLock)
            {
                if (connectionWatcher != null)
                {
                    if (!connectionWatcher.Join(5000))
                        connectionWatcherCts.Cancel();
                    connectionWatcher = null;
                }
                disconnectRequested = false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the serial port is connected.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return _serialPort != null && !gotReadWriteError && !disconnectRequested; }
        }

        /// <summary>
        /// Sets the serial port options.
        /// </summary>
        /// <param name="portName">Portname.</param>
        /// <param name="baudRate">Baudrate.</param>
        /// <param name="stopBits">Stopbits.</param>
        /// <param name="parity">Parity.</param>
        /// <param name="dataBits">Databits.</param>
        public void SetPort(string portName, int baudRate = 115200, StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight)
        {
            if (_portName != portName)
            {
                // set to error so that the connection watcher will reconnect
                // using the new port
                gotReadWriteError = true;
            }
            _portName = portName;
            _baudRate = baudRate;
            _stopBits = stopBits;
            _parity = parity;
            _dataBits = dataBits;
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
        /// <param name="message">Message.</param>
        public bool SendMessage(byte[] message)
        {
            bool success = false;
            if (IsConnected)
            {
                try
                {
                    _serialPort.Write(message, 0, message.Length);
                    success = true;
                    LogDebug(BitConverter.ToString(message));
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            return success;
        }

        #endregion

        #region Private members

        #region Serial Port handling

        private bool Open()
        {
            bool success = false;
            lock (accessLock)
            {
                Close();
                try
                {
                    bool tryOpen = true;

#if NETSTANDARD2_0
                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
                    var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
#endif
                    if (!isWindows)
                    {
                        tryOpen = (tryOpen && System.IO.File.Exists(_portName));
                    }
                    if (tryOpen)
                    {
#if NETSTANDARD2_0
                        _serialPort = new SerialPortStream();
#else
                        _serialPort = new SerialPort();
#endif
                        _serialPort.ErrorReceived += HandleErrorReceived;
                        _serialPort.PortName = _portName;
                        _serialPort.BaudRate = _baudRate;
                        _serialPort.StopBits = _stopBits;
                        _serialPort.Parity = _parity;
                        _serialPort.DataBits = (int)_dataBits;

                        // We are not using serialPort.DataReceived event for receiving data since this is not working under Linux/Mono.
                        // We use the readerTask instead (see below).
                        _serialPort.Open();
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                    Close();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    gotReadWriteError = false;
                    // Start the Reader task
                    readerCts = new CancellationTokenSource();
                    reader = new Thread(ReaderTask);
                    reader.Start(readerCts.Token);
                    OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(true));
                }
            }
            return success;
        }

        private void Close()
        {
            lock (accessLock)
            {
                // Stop the Reader task
                if (reader != null)
                {
                    if (!reader.Join(5000))
                        readerCts.Cancel();
                    reader = null;
                }
                if (_serialPort != null)
                {
                    _serialPort.ErrorReceived -= HandleErrorReceived;
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(false));
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                gotReadWriteError = true;
            }
        }

        private void HandleErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            LogError(e.EventType);
        }

        #endregion

        #region Background Tasks

        private void ReaderTask(object data)
        {
            var ct = (CancellationToken) data;
            while (IsConnected && !ct.IsCancellationRequested)
            {
                int msglen = 0;
                //
                try
                {
                    msglen = _serialPort.BytesToRead;
                    if (msglen > 0)
                    {
                        byte[] message = new byte[msglen];
                        //
                        int readbytes = 0;
                        while (_serialPort.Read(message, readbytes, msglen - readbytes) <= 0)
                            ; // noop
                        if (MessageReceived != null)
                        {
                            OnMessageReceived(new MessageReceivedEventArgs(message));
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                    gotReadWriteError = true;
                    Thread.Sleep(1000);
                }
            }
        }

        private void ConnectionWatcherTask(object data)
        {
            var ct = (CancellationToken) data;
            // This task takes care of automatically reconnecting the interface
            // when the connection is drop or if an I/O error occurs
            while (!disconnectRequested && !ct.IsCancellationRequested)
            {
                if (gotReadWriteError)
                {
                    try
                    {
                        Close();
                        // wait 1 sec before reconnecting
                        Thread.Sleep(1000);
                        if (!disconnectRequested)
                        {
                            try
                            {
                                Open();
                            }
                            catch (Exception e)
                            {
                                LogError(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                    }
                }
                if (!disconnectRequested)
                    Thread.Sleep(1000);
            }
        }

        private void LogDebug(string message)
        {
#if NET40 || NET461
            logger.Debug(message);
#else
            _logger.LogDebug(message);
#endif
        }

        private void LogError(Exception ex)
        {
#if NET40 || NET461
            logger.Error(ex);
#else
            _logger.LogError(ex, null);
#endif
        }

        private void LogError(SerialError error)
        {
#if NET40 || NET461
            logger.Error("SerialPort ErrorReceived: {0}", error);
#else
            _logger.LogError("SerialPort ErrorReceived: {0}", error);
#endif
        }

        #endregion

        #region Events Raising

        /// <summary>
        /// Raises the connected state changed event.
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
            LogDebug(args.Connected.ToString());
            if (ConnectionStatusChanged != null)
                ConnectionStatusChanged(this, args);
        }

        /// <summary>
        /// Raises the message received event.
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
        {
            LogDebug(BitConverter.ToString(args.Data));
            if (MessageReceived != null)
                MessageReceived(this, args);
        }

        #endregion

        #endregion

    }

}
