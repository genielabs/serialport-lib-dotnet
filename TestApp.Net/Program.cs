using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SerialPortLib;

namespace TestApp.NetCore
{
    class Program
    {
        private static string _defaultPort = "/dev/ttyUSB0";
        private static SerialPortInput _serialPort;

        public static void Main(string[] args)
        {
            var servicesProvider = BuildDi();
            using (servicesProvider as IDisposable)
            {
                _serialPort = servicesProvider.GetRequiredService<SerialPortInput>();
                _serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
                _serialPort.MessageReceived += SerialPort_MessageReceived;

                while (true)
                {
                    Console.WriteLine("\nPlease enter serial to open (eg. \"COM7\" or \"/dev/ttyUSB0\" without double quotes),");
                    Console.WriteLine("or enter \"QUIT\" to exit.\n");
                    Console.Write($"Port [{_defaultPort}]: ");
                    string port = Console.ReadLine();
                    if (String.IsNullOrWhiteSpace(port))
                        port = _defaultPort;
                    else
                        _defaultPort = port;

                    // exit if the user enters "quit"
                    if (port.Trim().ToLower().Equals("quit"))
                        break;

                    _serialPort.SetPort(port, 115200);
                    _serialPort.Connect();

                    Console.WriteLine($"Waiting for serial port connection on {port}.");
                    while (!_serialPort.IsConnected)
                    {
                        Console.Write(".");
                        Thread.Sleep(1000);
                    }
                    // This is a test message (Z-Wave protocol message for getting the nodes stored in the Controller)
                    var testMessage = new byte[] { 0x01, 0x03, 0x00, 0x02, 0xFE };
                    // Try sending some data if connected
                    if (_serialPort.IsConnected)
                    {
                        Console.WriteLine("\nConnected! Sending test message 5 times.");
                        for (int s = 0; s < 5; s++)
                        {
                            Thread.Sleep(2000);
                            Console.WriteLine($"\nSEND [{(s + 1)}]");
                            _serialPort.SendMessage(testMessage);
                        }
                    }
                    Console.WriteLine("\nTest sequence completed, now disconnecting.");

                    _serialPort.Disconnect();
                }
            }
        }

        static void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            // New message buffer received
            //Console.Write(System.Text.Encoding.UTF8.GetString(args.Data));
            Console.WriteLine(BitConverter.ToString(args.Data));
            // On every message received we send an ACK message back to the device
            _serialPort.SendMessage(new byte[] { 0x06 });
        }

        static void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            Console.WriteLine("Serial port connection status = {0}", args.Connected);
        }

        private static IServiceProvider BuildDi()
        {
            return new ServiceCollection()
                .AddTransient<SerialPortInput>()
                .BuildServiceProvider();
        }
    }
}
