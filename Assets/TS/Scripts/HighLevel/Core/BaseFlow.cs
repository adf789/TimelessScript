using UnityEngine;

public abstract class BaseFlow : ScriptableObject
{
    [SerializeField]
    protected UIType[] uis = null;

    public virtual GameState State { get; }
    public abstract void Enter();
    public abstract void Exit();
}
