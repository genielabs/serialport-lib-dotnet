/*
  This file is part of SerialPortLib (https://github.com/genielabs/serialport-lib-dotnet)
 
  Copyright (2012-2023) G-Labs (https://github.com/genielabs)

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

namespace SerialPortLib {

    public enum ConnectionEventType {
        Disconnected,
        DisconnectWithRetry,
        Connected
    }

    /// <summary>
    /// Connected state changed event arguments.
    /// </summary>
    public class ConnectionStatusChangedEventArgs
    {
        /// <summary>
        /// The connected state.
        /// </summary>
        public readonly bool Connected;
        
        /// <summary>
        /// Message
        /// </summary>
        public readonly ConnectionEventType ConnectionEventType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortLib.ConnectionStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="state">State of the connection (true = connected, false = not connected).</param>
        /// <param name="connectionEventType">Details on the state of the connection:            
        ///     Disconnected=The connection has been lost and the library will not attempt to reconnect,
        ///     DisconnectWithRetry=The connection has been lost and the library will attempt to reconnect,
        ///     Connected=The connection has been established
        /// </param>
        public ConnectionStatusChangedEventArgs(bool state, ConnectionEventType connectionEventType) {
            Connected = state;
            ConnectionEventType = connectionEventType;
        }
    }

    /// <summary>
    /// Message received event arguments.
    /// </summary>
    public class MessageReceivedEventArgs
    {
        /// <summary>
        /// The data.
        /// </summary>
        public readonly byte[] Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortLib.MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">Data.</param>
        public MessageReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
    }
    
    /// <summary>
    /// Message received event arguments from ReadLine.
    /// </summary>
    public class MessageReceivedLineEventArgs
    {
        /// <summary>
        /// The line string.
        /// </summary>
        public readonly string Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortLib.MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="line"></param>
        public MessageReceivedLineEventArgs(string Line)
        {
            Data =Line;
        }
    }
}

