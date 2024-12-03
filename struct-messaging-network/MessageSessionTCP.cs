using NetCoreServer;

namespace StructMessagingNetwork
{
    public class MessageSessionTCP : TcpSession
    {
        private MessageServerTCP messageServer;
        public MessageSessionTCP(MessageServerTCP server) : base(server)
        {
            messageServer = server;
        }

        protected override void OnConnected()
        {
            messageServer.OnClientConnected.Invoke(this);
        }

        protected override void OnDisconnected()
        {
            messageServer.OnClientDisconnected.Invoke(this);
        }

        public void SendMessage<T>(T message) where T : struct
        {
            if (StructParser.TryEncode(message, out byte[] result))
            {
                SendAsync(result);
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (StructParser.TryDecode(buffer, out var result))
            {
#pragma warning disable CS8604 // Result.value will not be null if TryDecode succeeds and returns true.
                messageServer.OnMessageReceived.Invoke(this, result.value);
#pragma warning restore CS8604 
            }
        }
    }
}