using System;
using System.Threading;

using SerialPortLib;
using NLog;

namespace Test.Serial
{
    class MainClass
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string defaultPort = "/dev/ttyUSB0";
        private static SerialPortInput serialPort;

        public static void Main(string[] args)
        {
            serialPort = new SerialPortInput();
            serialPort.ConnectedStateChanged += SerialPort_ConnectedStateChanged;
            serialPort.MessageReceived += SerialPort_MessageReceived;

            while (true)
            {
                Console.WriteLine("\nPlease enter serial to open (eg. \"COM7\" or \"/dev/ttyUSB0\" without double quotes),");
                Console.WriteLine("or enter \"QUIT\" to exit.\n");
                Console.Write("Port [{0}]: ", defaultPort);
                string port = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(port))
                    port = defaultPort;
                else
                    defaultPort = port;

                // exit if the user enters "quit"
                if (port.Trim().ToLower().Equals("quit"))
                    break;
            
                serialPort.SetPort(port, 115200);
                serialPort.Connect();

                Console.WriteLine("Waiting for serial port connection on {0}.", port);
                while (!serialPort.IsConnected)
                {
                    Console.Write(".");
                    Thread.Sleep(1000);
                }
                // This is a test message (ZWave protocol message for getting the nodes stored in the Controller)
                var testMessage = new byte[] { 0x01, 0x03, 0x00, 0x02, 0xFE };
                // Try sending some data if connected
                if (serialPort.IsConnected)
                {
                    Console.WriteLine("\nConnected! Sending test message 5 times.");
                    for (int s = 0; s < 5; s++)
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine("\nSEND [{0}]", (s + 1));
                        serialPort.SendMessage(testMessage);
                    }
                }
                Console.WriteLine("\nTest sequence completed, now disconnecting.");

                serialPort.Disconnect();
            }
        }

        static void SerialPort_MessageReceived(byte[] message)
        {
            logger.Debug(BitConverter.ToString(message));
            // On every message received we send an ACK message back to the device
            serialPort.SendMessage(new byte[] { 0x06 });
        }

        static void SerialPort_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            logger.Info("Connected = {0}", statusargs.Connected);
        }
    }
}
