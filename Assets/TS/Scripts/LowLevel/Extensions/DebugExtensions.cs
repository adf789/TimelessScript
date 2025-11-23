using UnityEngine;

public static class DebugExtensions
{
    public static void DebugLog<T>(this T obj, string message)
    {
#if DEBUG_LOG
        Debug.Log($"[{typeof(T).Name}]: {message}");
#endif
    }

    public static void DebugLogWarning<T>(this T obj, string message)
    {
#if DEBUG_LOG
        Debug.LogWarning($"[{typeof(T).Name}]: {message}");
#endif
    }

    public static void DebugLogError<T>(this T obj, string message)
    {
#if DEBUG_LOG
        Debug.LogError($"[{typeof(T).Name}]: {message}");
#endif
    }
}
