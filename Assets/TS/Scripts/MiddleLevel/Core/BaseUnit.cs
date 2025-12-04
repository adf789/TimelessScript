using UnityEngine;

public class BaseUnit<T> : BaseUnit where T : struct, IUnitModel
{
    protected T model;

    public ref T Model => ref model;

    public bool IsNullModel => default(T).Equals(model);

    public void SetModel(T value)
    {
        model = value;
    }
}


public abstract class BaseUnit : MonoBehaviour
{
    public virtual void Show()
    {

    }
}
