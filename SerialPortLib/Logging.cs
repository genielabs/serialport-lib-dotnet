using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Runtime.CompilerServices;

namespace SerialPortLib
{
    public static class Logging
    {
        private static readonly ILogger Logger;

        static Logging()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddSimpleConsole(options => configuration.GetSection("Logging:Console:FormatterOptions"));
                builder.AddConsole();
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            Logger = loggerFactory.CreateLogger("SerialPortInput");
        }

        public static void Log(LogLevel level, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Logger.LogWithCallInfo(level, message, null, memberName, filePath, lineNumber);
        }

        public static void LogInfo(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Information, message, memberName, filePath, lineNumber);
        }

        public static void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Debug, message, memberName, filePath, lineNumber);
        }

        public static void LogCritical(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Critical, message, memberName, filePath, lineNumber);
        }

        public static void LogTrace(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Trace, message, memberName, filePath, lineNumber);
        }

        public static void LogWarning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Warning, message, memberName, filePath, lineNumber);
        }

        public static void LogError(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Error, message, memberName, filePath, lineNumber);
        }

        public static void LogError(Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Logger.LogWithCallInfo(LogLevel.Error, ex.Message, ex, memberName, filePath, lineNumber);
        }

        public static void LogError(SerialError error, [CallerMemberName] string methodName = "")
        {
            Logger.LogError($"SerialPort error occurred in {methodName}: {error}");
        }

        // Extension to add caller info
        private static void LogWithCallInfo(this ILogger logger, LogLevel logLevel, string message, Exception exception = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       {"CallerMemberName", memberName},
                       {"CallerFilePath", filePath},
                       {"CallerLineNumber", lineNumber}
                   }))
            {
                logger.Log(logLevel, exception, "{Message}", message);
            }
        }
    }

}