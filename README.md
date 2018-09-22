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

SerialPortLib is open source software, licensed under the terms of GNU GPLV3 license. See the [LICENSE](LICENSE) file for details.
