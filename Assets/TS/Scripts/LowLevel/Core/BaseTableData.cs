using UnityEngine;

public abstract class BaseTableData : ScriptableObject
{
    public uint ID => id;
    public string Name => name;

    [Header("Basic Info")]
    [SerializeField] protected uint id;
    [SerializeField] protected new string name;
}