using UnityEngine;

public class BaseUnit<T> : BaseUnit where T : BaseModel
{
    public T Model { get => baseModel as T; }

    public void SetModel(T model)
    {
        baseModel = model;
    }
}


public abstract class BaseUnit : MonoBehaviour
{
    protected BaseModel baseModel = null;
}
