
using System;
using System.Collections.Generic;

public class ObserverSubManager : SubBaseManager<ObserverSubManager>
{
    private Dictionary<Type, Delegate> events = new Dictionary<Type, Delegate>();

    public void AddObserver<T>(Action<T> onEvent) where T : IObserverParam
    {
        Type type = typeof(T);

        if (events.TryGetValue(type, out var existingDelegate))
        {
            events[type] = Delegate.Combine(existingDelegate, onEvent);
        }
        else
        {
            events[type] = onEvent;
        }
    }

    // 제네릭 타입 명시 없이 사용 가능한 오버로드
    public void AddObserver(Delegate onEvent)
    {
        if (onEvent == null)
            return;

        Type paramType = ExtractActionParameterType(onEvent);
        if (paramType == null || !typeof(IObserverParam).IsAssignableFrom(paramType))
            return;

        if (events.TryGetValue(paramType, out var existingDelegate))
        {
            events[paramType] = Delegate.Combine(existingDelegate, onEvent);
        }
        else
        {
            events[paramType] = onEvent;
        }
    }

    public void RemoveObserver<T>(Action<T> onEvent) where T : IObserverParam
    {
        Type type = typeof(T);

        if (events.TryGetValue(type, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, onEvent);

            if (newDelegate == null)
                events.Remove(type);
            else
                events[type] = newDelegate;
        }
    }

    // 제네릭 타입 명시 없이 사용 가능한 오버로드
    public void RemoveObserver(Delegate onEvent)
    {
        if (onEvent == null)
            return;

        Type paramType = ExtractActionParameterType(onEvent);
        if (paramType == null)
            return;

        if (events.TryGetValue(paramType, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, onEvent);

            if (newDelegate == null)
                events.Remove(paramType);
            else
                events[paramType] = newDelegate;
        }
    }

    // Delegate에서 Action<T>의 T 타입을 추출
    private Type ExtractActionParameterType(Delegate del)
    {
        var delegateType = del.GetType();

        if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(Action<>))
        {
            return delegateType.GetGenericArguments()[0];
        }

        return null;
    }

    public void NotifyObserver<T>(T param) where T : IObserverParam
    {
        if (param == null)
            return;

        Type type = param.GetType();

        if (events.TryGetValue(type, out var eventDelegate))
        {
            (eventDelegate as Action<T>)?.Invoke(param);
        }
    }
}
