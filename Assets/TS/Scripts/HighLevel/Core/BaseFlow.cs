using UnityEngine;

public abstract class BaseFlow : ScriptableObject
{
    [SerializeField]
    protected UIType[] uis = null;

    public virtual GameState State { get; }
    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {

    }
}
