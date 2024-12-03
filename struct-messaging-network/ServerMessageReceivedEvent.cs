namespace StructMessagingNetwork
{
    internal class ServerMessageReceivedEvent
    {
        private List<Action<MessageSessionTCP, object>> OnReceivedMessageInternal = new List<Action<MessageSessionTCP, object>>();

        public void AddListener<T>(Action<MessageSessionTCP, T> listener) where T : struct
        {
            OnReceivedMessageInternal.Add((session, message) => {
                if (message.GetType().IsAssignableFrom(typeof(T)))
                {
                    T castedMessage = (T)message;
                    listener.Invoke(session, castedMessage);
                }
            });
        }

        public void Invoke(MessageSessionTCP session, object message)
        {
            OnReceivedMessageInternal.ForEach(a => a.Invoke(session, message));
        }
    }
}