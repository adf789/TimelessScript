using UnityEngine;

public abstract class BaseTableData : ScriptableObject
{
    public uint ID => id;
    
    [Header("Basic Info")]
    protected uint id;
}