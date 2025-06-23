using UnityEngine;

public class BaseManager<T> : BaseManager where T : BaseManager, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();

                if(instance == null)
                {
                    System.Type type = typeof(T);
                    string objectName = type.Name;
                    GameObject newObject = new GameObject(objectName);

                    instance = newObject.AddComponent<T>();
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        instance = this as T;
    }
}

public abstract class BaseManager : MonoBehaviour
{
    protected BaseManager() { }
}
