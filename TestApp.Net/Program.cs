using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using SerialPortLib;

namespace TestApp.NetCore
{
    class Program
    {
        private static string _defaultPort = "COM3";
        private static SerialPortInput _serialPort;

        // NOTE: To disable debug output uncomment the following two lines
        // NLog.LogLevel.Info;
        private static readonly NLog.LogLevel MinLogLevel = NLog.LogLevel.Debug;

        public static void Main(string[] args) {
            var servicesProvider = BuildDi();
            using (servicesProvider as IDisposable) {
                _serialPort = servicesProvider.GetRequiredService<SerialPortInput>();
                _serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
                _serialPort.MessageLineReceived += SerialPortOnMessageLineReceived;
                _serialPort.MessageReceived += SerialPort_MessageReceived;
                
                _serialPort.SetPort("COM3", 38400);
                _serialPort.Connect();

                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
                Console.WriteLine("Goodbye!");
                _serialPort.Disconnect();
                
            }
        }
        private static void SerialPortOnMessageLineReceived(Object sender, MessageReceivedLineEventArgs args) {
            Console.WriteLine(args.Data);
        }

        static void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.WriteLine("Received message: {0}", BitConverter.ToString(args.Data));
            // On every message received we send an ACK message back to the device
            _serialPort.SendMessage(new byte[] { 0x06 });
        }

        static void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args) {
            Console.WriteLine($"Serial port connection status = {args.Connected}, ConnectionEventType = {args.ConnectionEventType}");
            
        }

        private static IServiceProvider BuildDi()
        {
            return new ServiceCollection()
                .AddTransient<SerialPortInput>(provider =>new SerialPortInput(false))
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog(new LoggingConfiguration
                    {
                        LoggingRules =
                        {
                            new LoggingRule(
                                "*",
                                MinLogLevel,
                                new ConsoleTarget
                                {
                                    Layout = new SimpleLayout("${longdate} ${callsite} ${level} ${message} ${exception}")
                                })
                        }
                    });
                })
                .BuildServiceProvider();
        }

        private static void LibMain() {
            var servicesProvider = BuildDi();
            using (servicesProvider as IDisposable) {
                _serialPort = servicesProvider.GetRequiredService<SerialPortInput>();
                _serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
                _serialPort.MessageReceived += SerialPort_MessageReceived;

                while (true)
                {
                    Console.WriteLine("\nPlease enter serial to open (eg. \"COM7\" or \"/dev/ttyUSB0\" without double quotes),");
                    Console.WriteLine("or enter \"QUIT\" to exit.\n");
                    Console.Write("Port [{0}]: ", _defaultPort);
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

                    Console.WriteLine("Waiting for serial port connection on {0}.", port);
                    while (!_serialPort.IsConnected)
                    {
                        Console.Write(".");
                        Thread.Sleep(1000);
                    }
                    // This is a test message (ZWave protocol message for getting the nodes stored in the Controller)
                    var testMessage = new byte[] { 0x01, 0x03, 0x00, 0x02, 0xFE };
                    // Try sending some data if connected
                    if (_serialPort.IsConnected)
                    {
                        Console.WriteLine("\nConnected! Sending test message 5 times.");
                        for (int s = 0; s < 5; s++)
                        {
                            Thread.Sleep(2000);
                            Console.WriteLine("\nSEND [{0}]", (s + 1));
                            _serialPort.SendMessage(testMessage);
                        }
                    }
                    Console.WriteLine("\nTest sequence completed, now disconnecting.");

                    _serialPort.Disconnect();
                }
            }
        }
    }
}
