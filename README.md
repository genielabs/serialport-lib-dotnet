[![Build status](https://ci.appveyor.com/api/projects/status/r9fcyt98fjmygwu6?svg=true)](https://ci.appveyor.com/project/genemars/serialport-lib-dotnet)
[![NuGet](https://img.shields.io/nuget/v/SerialPortLib.svg)](https://www.nuget.org/packages/SerialPortLib/)
![License](https://img.shields.io/github/license/genielabs/serialport-lib-dotnet.svg)

# Serial Port library for .Net

## Features

- Easy to use
- Event driven
- Hot plug
- Automatically restabilish connection on error/disconnect
- Compatible with Mono
- It overcomes the lack of *DataReceived* event in Mono

## NuGet Package

SerialPortLib  is available as a [NuGet package](https://www.nuget.org/packages/SerialPortLib).

Run `Install-Package SerialPortLib` in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console) or search for “SerialPortLib” in your IDE’s package management plug-in.

## Example usage

```csharp
using SerialPortLib;
...
var serialPort = new SerialPortInput();

// Listen to Serial Port events

serialPort.ConnectionStatusChanged += delegate(object sender, ConnectionStatusChangedEventArgs args) 
{
    Console.WriteLine("Connected = {0}", args.Connected);
};

serialPort.MessageReceived += delegate(object sender, MessageReceivedEventArgs args)
{
    Console.WriteLine("Received message: {0}", BitConverter.ToString(args.Data));
};

// Set port options
serialPort.SetPort("/dev/ttyUSB0", 115200);

// Connect the serial port
serialPort.Connect();

// Send a message
var message = System.Text.Encoding.UTF8.GetBytes("Hello World!");
serialPort.SendMessage(message);
```

## License

SerialPortLib is open source software, licensed under the terms of Apache License 2.0. See the [LICENSE](LICENSE) file for details.

