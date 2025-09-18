/*
  This file is part of SerialPortLib (https://github.com/genielabs/serialport-lib-dotnet)

  Copyright (2012-2025) G-Labs (https://github.com/genielabs)

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
using System.IO.Ports;
using System.Runtime.CompilerServices; // Required for CallerMemberName
using System.Runtime.InteropServices;
using GLabs.Logging;

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

        private SerialPort _serialPort;
        private static readonly Logger Log = LogManager.GetLogger("SerialPortLib");


        private string _portName = "";
        private int _baudRate = 115200;
        private StopBits _stopBits = StopBits.One;
        private Parity _parity = Parity.None;
        private DataBits _dataBits = DataBits.Eight;

        // Read/Write error state variable
        private bool _gotReadWriteError = true;

        // Serial port reader task
        private Thread _reader;
        private CancellationTokenSource _readerCts;
        private readonly byte[] _buffer = new byte[2048];

        // Serial port connection watcher
        private static Timer _connectionWatcherTimer;


        private readonly object _serialPortLock = new object(); // Renamed for clarity
        private bool _disconnectRequested;

        private const int ReaderJoinTimeoutMs = 5000;
        private const int ConnectionWatcherSleepMs = 1000;
        private const int DefaultReconnectDelayMs = 1000;
        private const int DefaultReadWriteTimeoutMs = 5000;

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

        public SerialPortInput()
        {
            _readerCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Connect to the serial port.
        /// </summary>
        public bool Connect()
        {
            if (_disconnectRequested)
            {
                return false;
            }
            lock (_serialPortLock)
            {
                Disconnect();
                Open();
                _connectionWatcherTimer = new Timer(ConnectionWatcherTask, null, ConnectionWatcherSleepMs, ConnectionWatcherSleepMs);
            }
            return IsConnected;
        }

        /// <summary>
        /// Disconnect the serial port.
        /// </summary>
        public void Disconnect()
        {
            if (_disconnectRequested)
            {
                return;
            }
            _disconnectRequested = true;
            Close();
            lock (_serialPortLock)
            {
                if (_connectionWatcherTimer != null)
                {
                    _connectionWatcherTimer.Dispose();
                    _connectionWatcherTimer = null;
                }
                _disconnectRequested = false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the serial port is connected.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return _serialPort != null && _serialPort.IsOpen && !_gotReadWriteError && !_disconnectRequested; }
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
            if (_portName != portName || _baudRate != baudRate || stopBits != _stopBits || parity != _parity || dataBits != _dataBits)
            {
                // Change Parameters request
                // Take into account immediately the new connection parameters
                // (do not use the ConnectionWatcher, otherwise strange things will occurs !)
                _portName = portName;
                _baudRate = baudRate;
                _stopBits = stopBits;
                _parity = parity;
                _dataBits = dataBits;
                if (IsConnected)
                {
                    Connect();      // Take into account immediately the new connection parameters
                }
                Log.Debug($"Port parameters changed (port name {_portName} / baudrate {_baudRate} / stopbits {_stopBits} / parity {_parity} / databits {_dataBits})");
            }
        }

        /// <summary>
        /// Returns the underlying System.IO.Ports.SerialPort instance 
        /// </summary>
        /// <returns></returns>
        public SerialPort SerialPort
        {
            get
            {
                if (_serialPort == null)
                {
                    _serialPort = new SerialPort();
                }
                return _serialPort;
            }
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
                    Log.Debug(BitConverter.ToString(message));
                }
                catch (ObjectDisposedException e)
                {
                    _gotReadWriteError = true;
                    Log.Error(e);
                }
                catch (TimeoutException e)
                {
                    _gotReadWriteError = true;
                    Log.Error(e);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            return success;
        }

        /// <summary>
        /// Gets/Sets serial port reconnection delay in milliseconds.
        /// </summary>
        public int ReconnectDelay { get; set; } = DefaultReconnectDelayMs;
        
        /// <summary>
        /// Gets or sets the serial port read/write timeout.
        /// </summary>
        public int Timeout { get; set; } = DefaultReadWriteTimeoutMs;

        #endregion

        #region Private members

        #region Serial Port handling

        private bool Open()
        {
            bool success = false;
            lock (_serialPortLock)
            {
                Close();
                try
                {
                    bool tryOpen = true;

                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (!isWindows)
                    {
                        tryOpen = (tryOpen && System.IO.File.Exists(_portName));
                    }
                    if (tryOpen)
                    {
                        SerialPort.ErrorReceived += HandleErrorReceived;
                        SerialPort.PortName = _portName;
                        SerialPort.BaudRate = _baudRate;
                        SerialPort.StopBits = _stopBits;
                        SerialPort.Parity = _parity;
                        SerialPort.DataBits = (int)_dataBits;
                        SerialPort.ReadTimeout = Timeout;
                        SerialPort.WriteTimeout = Timeout;

                        // We are not using serialPort.DataReceived event for receiving data since this is not working under Linux/Mono.
                        // We use the readerTask instead (see below).

                        SerialPort.Open();
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Close();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _gotReadWriteError = false;
                    // Start the Reader task
                    _readerCts = new CancellationTokenSource();
                    _reader = new Thread(ReaderTask) { IsBackground = true };
                    _reader.Start(_readerCts.Token);
                    OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(true));
                }
            }
            return success;
        }

        private void Close()
        {
            lock (_serialPortLock)
            {
                // Stop the Reader task
                if (_reader != null)
                {
                    if (!_reader.Join(ReaderJoinTimeoutMs))
                        _readerCts.Cancel();
                    _reader = null;
                }
                if (_serialPort != null)
                {
                    _serialPort.ErrorReceived -= HandleErrorReceived;
                    if (_serialPort.IsOpen)
                    {
                        try
                        {
                            _serialPort.Close();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex); // Log the exception during close
                        }
                        if (!_serialPort.IsOpen)
                        {
                            OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(false));
                        }
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                _gotReadWriteError = true;
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
                try
                {
                    var readerTask = _serialPort?.BaseStream.ReadAsync(_buffer, 0, _buffer.Length, ct);
                    readerTask?.Wait();
                    if (readerTask?.Result > 0)
                    {
                        byte[] received = new byte[readerTask.Result];
                        Buffer.BlockCopy(_buffer, 0, received, 0, readerTask.Result);
                        OnMessageReceived(new MessageReceivedEventArgs(received));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task was cancelled, exit gracefully
                    break;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    _gotReadWriteError = true;
                    Thread.Sleep(DefaultReconnectDelayMs);
                }
            }
        }

        private void ConnectionWatcherTask(object data)
        {
            // This task takes care of automatically reconnecting the interface
            // when the connection is drop or if an I/O error occurs
            if (!_disconnectRequested && _gotReadWriteError)
            {
                try
                {
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        Close();
                    }
                    else
                    {
                        Open();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private void LogError(SerialError error, [CallerMemberName] string methodName = "")
        {
            Log.Error("SerialPort error occurred in {MethodName}: {Error}", methodName, error);
        }

        #endregion

        #region Events Raising

        /// <summary>
        /// Raises the connected state changed event.
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
            Log.Debug(args.Connected.ToString());
            ConnectionStatusChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the message received event.
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
        {
            Log.Debug(BitConverter.ToString(args.Data));
            MessageReceived?.Invoke(this, args);
        }

        #endregion

        #endregion

    }
}
