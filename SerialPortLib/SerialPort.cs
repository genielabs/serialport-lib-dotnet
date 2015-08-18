/*
    This file is part of SerialPortLib source code.

    SerialPortLib is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SerialPortLib is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SerialPortLib.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://github.com/genielabs/serialport-lib-dotnet
 */

using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

using NLog;

namespace SerialPortLib
{
    /// <summary>
    /// Serial port I/O
    /// </summary>
    public class SerialPortInput
    {
        
        #region Private Fields

        internal static Logger logger = LogManager.GetCurrentClassLogger();

        private SerialPort serialPort;
        private string portName = "";
        private int baudRate = 115200;

        // Read/Write error state variable
        private bool gotReadWriteError = true;
        private bool isConnected = false;

        // Serial port reader task
        private Task readerTask;
        private CancellationTokenSource readerTokenSource;
        // Serial port connection watcher
        private Task connectionWatcher;
        private CancellationTokenSource watcherTokenSource;

        #endregion

        #region Public Events

        /// <summary>
        /// Connected state changed event.
        /// </summary>
        public delegate void ConnectionStatusChangedEvent(object sender, ConnectionStatusChangedEventArgs args);
        /// <summary>
        /// Occurs when connected state changed.
        /// </summary>
        public event ConnectionStatusChangedEvent ConnectionStatusChanged;

        /// <summary>
        /// Message received event.
        /// </summary>
        public delegate void MessageReceivedEvent(object sender, MessageReceivedEventArgs args);
        /// <summary>
        /// Occurs when message received.
        /// </summary>
        public event MessageReceivedEvent MessageReceived;

        #endregion

        #region Public Members

        /// <summary>
        /// Connect to the serial port.
        /// </summary>
        public bool Connect()
        {
            Disconnect();
            bool returnValue = Open();
            gotReadWriteError = !returnValue;
            watcherTokenSource = new CancellationTokenSource();
            connectionWatcher = Task.Factory.StartNew(() => ConnectionWatcherTask(watcherTokenSource.Token), watcherTokenSource.Token);
            return returnValue;
        }

        /// <summary>
        /// Disconnect the serial port.
        /// </summary>
        public void Disconnect()
        {
            if (connectionWatcher != null)
            {
                watcherTokenSource.Cancel();
                connectionWatcher.Wait(5000);
                if (connectionWatcher != null)
                    connectionWatcher.Dispose();
                connectionWatcher = null;
                watcherTokenSource = null;
            }
            Close();
        }

        /// <summary>
        /// Gets a value indicating whether the serial port is connected.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return isConnected && !gotReadWriteError; }
        }

        /// <summary>
        /// Sets the serial port options.
        /// </summary>
        /// <param name="portname">Portname.</param>
        /// <param name="baudrate">Baudrate.</param>
        public void SetPort(string portname, int baudrate = 115200)
        {
            if (portName != portname && serialPort != null)
            {
                Close();
            }
            portName = portname;
            baudRate = baudrate;
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
        /// <param name="message">Message.</param>
        public bool SendMessage(byte[] message)
        {
            bool success = false;
            try
            {
                serialPort.Write(message, 0, message.Length);
                success = true;
                logger.Debug(BitConverter.ToString(message));
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
            return success;
        }

        #endregion

        #region Private members

        #region Serial Port handling

        private bool Open()
        {
            Close();
            bool success = false;
            try
            {
                bool tryOpen = (serialPort == null);
                if (Environment.OSVersion.Platform.ToString().StartsWith("Win") == false)
                {
                    tryOpen = (tryOpen && System.IO.File.Exists(portName));
                }
                if (tryOpen)
                {
                    serialPort = new SerialPort();
                    serialPort.PortName = portName;
                    serialPort.BaudRate = baudRate;
                    serialPort.ErrorReceived += HanldeErrorReceived;
                    // We are not using serialPort.DataReceived event for receiving data since this is not working under Linux/Mono.
                    // We use the readerTask instead (see below).
                    if (serialPort.IsOpen == false)
                    {
                        serialPort.Open();
                        success = true;
                    }
                }
            }
            catch (Exception e)
            { 
                logger.Error(e);
            }
            isConnected = success;
            if (isConnected)
            {
                // Start the Reader task
                readerTokenSource = new CancellationTokenSource();
                readerTask = Task.Factory.StartNew(() => ReaderTask(readerTokenSource.Token), readerTokenSource.Token);
                OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(isConnected));
            }
            return success;
        }

        private void Close()
        {
            if (serialPort != null)
            {
                try
                {
                    serialPort.Close();
                    serialPort.ErrorReceived -= HanldeErrorReceived;
                }
                catch (Exception e)
                { 
                    logger.Error(e);
                }
                serialPort = null;
                if (isConnected)
                    OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(false));
            }
            // Stop the Reader task
            if (readerTask != null)
            {
                readerTokenSource.Cancel();
                readerTask.Wait(5000);
                if (readerTask != null)
                    readerTask.Dispose();
                readerTask = null;
                readerTokenSource = null;
            }
            isConnected = false;
        }

        private void HanldeErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            logger.Error(e.EventType);
        }

        #endregion

        #region Background Tasks

        private void ReaderTask(CancellationToken readerToken)
        {
            while (!readerToken.IsCancellationRequested)
            {
                int msglen = 0;
                //
                if (serialPort != null)
                {
                    try
                    {
                        msglen = serialPort.BytesToRead;
                        if (msglen > 0)
                        {
                            byte[] message = new byte[msglen];
                            //
                            int readbytes = 0;
                            while (serialPort.Read(message, readbytes, msglen - readbytes) <= 0)
                                ; // noop
                            logger.Debug(BitConverter.ToString(message));
                            if (MessageReceived != null)
                            {
                                // Prevent event listeners from blocking the receiver task
                                new Thread(() => OnMessageReceived(new MessageReceivedEventArgs(message))).Start();
                                //ThreadPool.QueueUserWorkItem(new WaitCallback(OnMessageReceived), new MessageReceivedEventArgs(message));
                            }
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }
                    catch (Exception e)
                    {
                        gotReadWriteError = true;
                        logger.Error(e);
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private void ConnectionWatcherTask(CancellationToken watcherToken)
        {
            // This task takes care of automatically reconnecting the interface
            // when the connection is drop or if an I/O error occurs
            while (!watcherToken.IsCancellationRequested)
            {
                if (gotReadWriteError)
                {
                    try
                    {
                        Close();
                        // wait 1 sec before reconnecting
                        Thread.Sleep(1000);
                        if (!watcherToken.IsCancellationRequested)
                        {
                            try
                            {
                                gotReadWriteError = !Open();
                            }
                            catch (Exception e)
                            { 
                                logger.Error(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region Events Raising

        /// <summary>
        /// Raises the connected state changed event.
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
            if (ConnectionStatusChanged != null)
                ConnectionStatusChanged(this, args);
        }

        /// <summary>
        /// Raises the message received event.
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
        {
            if (MessageReceived != null)
                MessageReceived(this, args);
        }

        #endregion

        #endregion

    }

}
