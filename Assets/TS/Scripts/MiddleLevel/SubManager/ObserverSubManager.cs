
using System;
using System.Collections.Generic;

public class ObserverSubManager : SubBaseManager<ObserverSubManager>
{
    private Dictionary<Type, Action<IObserverParam>> events = new Dictionary<Type, Action<IObserverParam>>();

    public void AddObserver<T>(Action<IObserverParam> onEvent) where T : IObserverParam
    {
        Type type = typeof(T);

        if (events.TryGetValue(type, out var action))
        {
            action += onEvent;
        }
        else
        {
            events[type] = onEvent;
        }
    }

    public void RemoveObserver<T>(Action<IObserverParam> onEvent)
    {
        Type type = typeof(T);

        if (events.TryGetValue(type, out var action))
        {
            action -= onEvent;

            if (action == null)
                events.Remove(type);
        }
    }

    public void NotifyObserver<T>(T param) where T : IObserverParam
    {
        if (param == null)
            return;

        Type type = param.GetType();

        if (events.TryGetValue(type, out var action))
            action?.Invoke(param);
    }
}
