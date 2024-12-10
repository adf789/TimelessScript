using UnityEngine;

public class BaseView<T> : BaseView where T : BaseModel
{
    public T Model { get => baseModel as T; }

    public void SetModel(T model)
    {
        baseModel = model;
    }
}


public abstract class BaseView : MonoBehaviour
{
    protected BaseModel baseModel = null;
}
