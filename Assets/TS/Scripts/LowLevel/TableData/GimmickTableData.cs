
using UnityEngine;

public class GimmickTableData : BaseTableData
{
    public uint AcquireItem => acquireItem;
    public long AcquireCount => acquireCount;

    [SerializeField] private uint acquireItem;
    [SerializeField] private long acquireCount;
}
