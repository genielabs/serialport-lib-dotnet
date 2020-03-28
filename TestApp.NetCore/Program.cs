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
        private static string defaultPort = "/dev/ttyUSB0";
        private static SerialPortInput serialPort;

        // NOTE: To disable debug output uncomment the following two lines
        // NLog.LogLevel.Info;
        private static NLog.LogLevel minLogLevel = NLog.LogLevel.Debug;

        public static void Main(string[] args)
        {
            var servicesProvider = BuildDi();
            using (servicesProvider as IDisposable)
            {
                serialPort = servicesProvider.GetRequiredService<SerialPortInput>();
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
                            Thread.Sleep(2000);
                            Console.WriteLine("\nSEND [{0}]", (s + 1));
                            serialPort.SendMessage(testMessage);
                        }
                    }
                    Console.WriteLine("\nTest sequence completed, now disconnecting.");

                    serialPort.Disconnect();
                }
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

        private static IServiceProvider BuildDi()
        {
            return new ServiceCollection()
                .AddTransient<SerialPortInput>()
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
                                minLogLevel,
                                new ConsoleTarget
                                {
                                    Layout = new SimpleLayout("${longdate} ${callsite} ${level} ${message} ${exception}")
                                })
                        }
                    });
                })
                .BuildServiceProvider();
        }
    }
}
