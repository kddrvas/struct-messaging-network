using System.Diagnostics;
using System.Net;
using NetCoreServer;

namespace StructMessagingNetwork
{
    public enum ClientConnectionOperationResult
    {
        Success,
        AlreadyConnected,
        AlreadyConnecting,
        TimedOut,
        Unknown
    }

    public class MessageClientTCP : TcpClient
    {
        private ClientMessageReceivedEvent OnMessageReceived = new();
        public Event OnConnectionTimeOut = new();
        public Event OnConnect = new();
        public Event OnDisconnect = new();
        
        public MessageClientTCP(string address, int port) : base(address, port)
        {
        }

        protected override void OnConnected()
        {
            OnConnect.Invoke();
        }

        protected override void OnDisconnected()
        {
            OnDisconnect.Invoke();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (StructParser.TryDecode(buffer, out var result))
            {
#pragma warning disable CS8604 // Result.value will not be null if TryDecode succeeds and returns true.
                OnMessageReceived.Invoke(result.value);
#pragma warning restore CS8604 
            }
        }

        public void SendMessage<T>(T message) where T : struct
        {
            if (StructParser.TryEncode(message, out byte[] result))
            {
                SendAsync(result);
            }
        }

        public void HandleMessage<T>(Action<T> handler) where T : struct
        {
            OnMessageReceived.AddListener(handler);
        }

        public bool SimpleCommandLoop(out string command, bool requiresConnection = true)
        {
            command = "";
            while (IsConnected || !requiresConnection)
            {
                Console.Write("Client Terminal [>:] ");
                string? maybeCommand = Console.ReadLine();
                
                if (maybeCommand != null)
                {
                    command = maybeCommand;
                    break;
                }
            }

            return IsConnected || !requiresConnection;
        }

        public ClientConnectionOperationResult ConnectAsyncWithTimeout(int timeout = 3000)
        {
            if (ConnectAsync())
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                while (!IsConnected)
                {
                    if (stopwatch.ElapsedMilliseconds == timeout)
                    {
                        OnConnectionTimeOut.Invoke();
                        return ClientConnectionOperationResult.TimedOut;
                    }
                }

                return ClientConnectionOperationResult.Success;
            }
            else if (IsConnected)
                return ClientConnectionOperationResult.AlreadyConnected;
            else if (IsConnecting)
                return ClientConnectionOperationResult.AlreadyConnecting;

            return ClientConnectionOperationResult.Unknown;
        }
    }
}