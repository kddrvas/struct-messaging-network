namespace StructMessagingNetwork
{
    internal class ClientMessageReceivedEvent
    {
        private List<Action<object>> OnReceivedMessageInternal = new();

        public void AddListener<T>(Action<T> listener) where T : struct
        {
            OnReceivedMessageInternal.Add((message) => {
                if (message.GetType().IsAssignableFrom(typeof(T)))
                {
                    T castedMessage = (T)message;
                    listener.Invoke(castedMessage);
                }
            });
        }

        public void Invoke(object message)
        {
            OnReceivedMessageInternal.ForEach(a => a.Invoke(message));
        }
    }
}