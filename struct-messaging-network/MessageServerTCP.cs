using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace StructMessagingNetwork
{
    public class MessageServerTCP : TcpServer
    {
        internal ServerMessageReceivedEvent OnMessageReceived = new ServerMessageReceivedEvent();
        public Event<MessageSessionTCP> OnClientConnected = new Event<MessageSessionTCP>();
        public Event<MessageSessionTCP> OnClientDisconnected = new Event<MessageSessionTCP>();
        public Event<SocketError> OnSocketError = new Event<SocketError>();

        public MessageServerTCP(int port) : base(IPAddress.Any, port)
        {
        }

        public IEnumerable<MessageSessionTCP> GetAllSessions()
        {
            return Sessions.Values.Cast<MessageSessionTCP>();
        }

        protected override TcpSession CreateSession()
        {
            return new MessageSessionTCP(this);
        }

        protected override void OnError(SocketError error)
        {
            OnSocketError.Invoke(error);
        }

        public void HandleMessage<T>(Action<MessageSessionTCP, T> handler) where T : struct
        {
            OnMessageReceived.AddListener(handler);
        }

        public bool SimpleCommandLoop(out string command, bool requiresConnection = true)
        {
            command = "";
            while (IsStarted || !requiresConnection)
            {
                Console.Write("Server Terminal [>:] ");
                string? maybeCommand = Console.ReadLine();
                
                if (maybeCommand != null)
                {
                    command = maybeCommand;
                    break;
                }
            }

            return IsStarted || !requiresConnection;
        }
    }
}