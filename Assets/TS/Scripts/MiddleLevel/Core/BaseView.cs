using UnityEngine;

public class BaseView<T> : BaseView where T : BaseModel
{
    public T Model { get => baseModel as T; }
}


public abstract class BaseView : MonoBehaviour
{
    protected BaseModel baseModel = null;

    public void SetModel(BaseModel model)
    {
        baseModel = model;
    }

    public virtual void Show()
    {

    }
}
