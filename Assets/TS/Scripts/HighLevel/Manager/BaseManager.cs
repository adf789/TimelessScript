using UnityEngine;

public class BaseManager<T> : BaseManager where T : BaseManager, new()
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    System.Type type = typeof(T);
                    string objectName = type.Name;
                    GameObject newObject = new GameObject(objectName);

                    _instance = newObject.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this as T;
    }
}

public abstract class BaseManager : MonoBehaviour
{
    protected BaseManager() { }
}