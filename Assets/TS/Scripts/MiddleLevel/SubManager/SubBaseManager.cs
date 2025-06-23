using UnityEngine;

public class SubBaseManager<T> : SubBaseManager where T : SubBaseManager, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if(instance == null)
                instance = new T();

            return instance;
        }
    }
}

public class SubBaseManager
{
    protected SubBaseManager() { }
}
