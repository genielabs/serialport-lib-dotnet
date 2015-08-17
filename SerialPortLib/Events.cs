using System;

namespace SerialPortLib
{

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
        /// Initializes a new instance of the <see cref="SerialPortLib.ConnectionStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="state">State of the connection (true = connected, false = not connected).</param>
        public ConnectionStatusChangedEventArgs(bool state)
        {
            Connected = state;
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
}

