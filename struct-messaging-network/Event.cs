namespace StructMessagingNetwork
{
    public class Event<T>
    {
        private readonly HashSet<Action<T>> handlers = new();

        public void Invoke(T param)
        {
            foreach (var handler in handlers)
            {
                handler.Invoke(param);
            }
        }

        public void AddListener(Action<T> listener)
        {
            handlers.Add(listener);
        }

        public void RemoveListener(Action<T> listener)
        {
            handlers.Remove(listener);
        }

        public static Event<T> operator +(Event<T> ev, Action<T> handler)
        {
            ev.handlers.Add(handler);
            return ev;
        }

        public static Event<T> operator -(Event<T> ev, Action<T> handler)
        {
            ev.handlers.Remove(handler);
            return ev;
        }
    }

    public class Event
    {
        private readonly HashSet<Action> handlers = new();

        public void Invoke()
        {
            foreach (var handler in handlers)
            {
                handler.Invoke();
            }
        }

        public void AddListener(Action listener)
        {
            handlers.Add(listener);
        }

        public void RemoveListener(Action listener)
        {
            handlers.Remove(listener);
        }

        public static Event operator +(Event ev, Action handler)
        {
            ev.handlers.Add(handler);
            return ev;
        }

        public static Event operator -(Event ev, Action handler)
        {
            ev.handlers.Remove(handler);
            return ev;
        }
    }

}