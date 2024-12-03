# Struct Messaging Network 
This is a C# wrapper around [NetCoreServer](https://github.com/chronoxor/NetCoreServer) which allows communicating over the network by using arbitrary structs as messages. It works primarily by enumerating through every public member of a struct instance and individually converting them to and from a bytestream.

This greatly simplifies the process of capturing meaningful network data for the programmer and allows for the creation of a fully functional server in only a few minutes of time.

**Note that this tool has only been tested on Windows.**

The meat of this project can is found in the StructParser file within the main project. In it you can find the parsing implementation for all currently supported types, listed here:

* int
* float
* double
* char
* char[]
* string
* IPAddress

This list will expand overtime.

## Minimal Server Example
```c#
using StructMessagingNetwork;

public class Program
{
    struct ExampleMessage
    {
        public int value1;
        public string value2;
        public float value3;
    }
    
    public static void Main(string[] args)
    {
        // Create server by providing a port.
        MessageServerTCP = new MessageServerTCP(1111);
        
        // Set up handlers. Each receive the client session who sent the message 
        // as well as the message itself.
        server.HandleMessage<ExampleMessage>((clientSession, exampleMessage) => {
            // ...
        });
        
        // Start the server.
        if (server.Start())
        {
            // Helper function for console commands.
            while(server.SimpleCommandLoop(out string command))
            {
                if (command == "quit")
                {
                    server.Stop();
                }
            }
        }
    }
}
```

## Registering Custom Parsers
```c#
using StructMessagingNetwork;

public class Program
{
    public static void Main(string[] args)
    {
        // Example implementation for bool. You need to provide two functions.
        // First: Object -> byte[]
        // Second: (byte[], int) -> (Object, int)
        //   The int input parameter is an index, which represents where in the
        //   buffer we should start to read. The int output parameter represents
        //   how many bytes we have read.
        StructParser.RegisterCustomParser<bool>(
            obj => {
                return BitConverter.GetBytes((bool)obj);
            },
            (buffer, index) => {
                return (BitConverter.ToInt32(buffer, index), sizeof(bool));
            }
        );
    }
}
```
