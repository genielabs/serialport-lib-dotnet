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

using System;
using System.Threading;

using SerialPortLib;
using NLog;

namespace Test.Serial
{
    class MainClass
    {
        private static string defaultPort = "/dev/ttyUSB0";
        private static SerialPortInput serialPort;

        public static void Main(string[] args)
        {
            // NOTE: To disable debug output uncomment the following two lines
            //LogManager.Configuration.LoggingRules.RemoveAt(0);
            //LogManager.Configuration.Reload();

            serialPort = new SerialPortInput();
            serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
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

        static void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.WriteLine("Received message: {0}", BitConverter.ToString(args.Data));
            // On every message received we send an ACK message back to the device
            serialPort.SendMessage(new byte[] { 0x06 });
        }

        static void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            Console.WriteLine("Serial port connection status = {0}", args.Connected);
        }
    }
}
