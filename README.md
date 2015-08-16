# Serial Port libray for .Net

## Features

- Easy to use
- Event driven
- Hot plug
- Automatically restabilish connection on error/disconnect
- Compatible with Mono
- It overcomes the lack of *DataReceived* event in Mono

## Example usage

    using SerialPortLib;
    ...
    var serialPort = new SerialPortInput();

    // Listen to Serial Port events
    serialPort.ConnectedStateChanged += delegate(object sender, ConnectedStateChangedEventArgs statusargs) {
        logger.Info("Connected = {0}", statusargs.Connected);
    };
    serialPort.MessageReceived += delegate(byte[] message) {
        logger.Debug(BitConverter.ToString(message));
    };

    // Set port options
    serialPort.SetPort("/dev/ttyUSB0", 115200);

    // Connect the serial port
    serialPort.Connect();

    // Send a message
    var message = System.Text.Encoding.UTF8.GetBytes("Hello World!");
    serialPort.SendMessage(message);


## License

SerialPortLib is open source software, licensed under the terms of GNU GPLV3 license. See the [LICENSE](LICENSE) file for details.
