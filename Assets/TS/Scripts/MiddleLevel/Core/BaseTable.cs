using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTable<T> : BaseTable where T : BaseTableData
{
    [Header("ID Range Settings")]
    [SerializeField] protected uint idBandwidth = 100000;

    [Tooltip("이 대역폭에서 사용할 ID 개수")]
    [SerializeField] protected uint idRangeSize = 100000;

    [Space]
    [SerializeField] protected List<T> datas = new List<T>();

    private Dictionary<uint, T> dataDic;

    public virtual void Load()
    {
        Initialize();
    }

    public void Initialize()
    {
        dataDic = new Dictionary<uint, T>();

        foreach (var data in datas)
        {
            if (data != null && !dataDic.ContainsKey(data.ID))
            {
                dataDic[data.ID] = data;
            }
        }
    }

    public T Get(uint id)
    {
        if (dataDic == null)
            Initialize();

        return dataDic.TryGetValue(id, out var data) ? data : null;
    }

    public bool IsValid(uint id)
    {
        return Get(id) != null;
    }

    public List<T> GetAllDatas()
    {
        return datas;
    }

    public int GetDataCount()
    {
        return datas != null ? datas.Count : 0;
    }

    public uint GetNextAutoID()
    {
        var usedIDs = new HashSet<uint>();

        foreach (var data in datas)
        {
            if (data != null)
                usedIDs.Add(data.ID);
        }

        uint actualStart = GetActualRangeStart();
        uint actualEnd = GetActualRangeEnd();

        uint nextId = actualStart + 1;

        while (nextId < actualEnd && usedIDs.Contains(nextId))
        {
            nextId++;
        }

        if (nextId >= actualEnd)
        {
            Debug.LogError($"ID range exhausted for {GetType().Name}! Range: {actualStart + 1}~{actualEnd - 1}");
            return 0;
        }

        return nextId;
    }

    public bool IsInIDRange(uint id)
    {
        uint actualStart = GetActualRangeStart();
        uint actualEnd = GetActualRangeEnd();
        return id > actualStart && id < actualEnd;
    }

    public string GetIDRangeInfo()
    {
        uint actualStart = GetActualRangeStart();
        uint actualEnd = GetActualRangeEnd();
        return $"{actualStart + 1:N0} ~ {actualEnd - 1:N0} (Bandwidth: {idBandwidth:N0})";
    }

    private uint GetActualRangeStart()
    {
        // 대역폭을 기준으로 실제 시작 ID 계산
        return idBandwidth;
    }

    private uint GetActualRangeEnd()
    {
        // 대역폭 + 범위 크기로 실제 끝 ID 계산
        return idBandwidth + idRangeSize;
    }
}

public abstract class BaseTable : ScriptableObject
{
    
}
