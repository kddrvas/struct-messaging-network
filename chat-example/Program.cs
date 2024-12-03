namespace StructMessagingNetwork 
{
    public class Program 
    {
        struct ClientChatMessage
        {
            public string message;
        }

        public static void Main(string[] args) 
        {
            // Server
            if (args.Length == 0 || args[0].ToLower() == "server")
            {
                MessageServerTCP server = new MessageServerTCP(1111);

                server.HandleMessage<ClientChatMessage>((clientSession, chatMessage) => {
                    Console.WriteLine("Message From Client: " + chatMessage.message);

                    foreach(MessageSessionTCP session in server.GetAllSessions())
                    {
                        if (clientSession != session)
                            session.SendMessage(chatMessage);
                    }
                });

                server.OnClientConnected += session => {
                    Console.WriteLine("A new client connected.");
                };

                server.OnClientDisconnected += session => {
                    Console.WriteLine("A client disconnected.");
                };

                //
                
                Console.WriteLine("Starting server...");
                if (server.Start())
                {
                    Console.WriteLine("Server started.");

                    // Command loop
                    while(server.SimpleCommandLoop(out string command))
                    {
                        if (command == "quit")
                        {
                            break;
                        }
                    }

                    Console.WriteLine("Server shutting down...");
                    server.Stop();
                    Console.WriteLine("Server stopped...");
                }
                else
                {
                    Console.WriteLine("Server is already started.");
                }
            }
            // Client
            else
            {
                MessageClientTCP client = new MessageClientTCP("127.0.0.1", 1111);
                client.HandleMessage<ClientChatMessage>(chatMessage => {
                    Console.WriteLine("Other Client: " + chatMessage.message);
                });

                //

                Console.WriteLine("Connecting to server...");
                var operationResult = client.ConnectAsyncWithTimeout();
                if (operationResult == ClientConnectionOperationResult.Success)
                {
                    Console.WriteLine("Connected to server.");

                    while(client.SimpleCommandLoop(out string command))
                    {
                        if (command == "/quit")
                        {
                            break;
                        }

                        ClientChatMessage testMessage = new ClientChatMessage { message = command };
                        client.SendMessage(testMessage);

                        Console.WriteLine("You: " + command);
                    }
                }
                else
                    Console.WriteLine("Coud not connect to server: " + operationResult);

                //

                if (client.IsConnected)
                {
                    Console.WriteLine("Disconnecting from server...");
                    client.DisconnectAsync();
                    Console.WriteLine("Disconnected from server.");
                }
            }

            return;
        }
    }
}

