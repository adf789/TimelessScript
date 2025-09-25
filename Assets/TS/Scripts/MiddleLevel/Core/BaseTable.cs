using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTable<T> : BaseTable where T : BaseTableData
{
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

#if UNITY_EDITOR
    [ContextMenu("Add Data")]
    public virtual void AddData()
    {
        var newResourcesPath = CreateInstance<T>();
        newResourcesPath.name = "Added Data";

        // Save as sub-asset
        UnityEditor.AssetDatabase.AddObjectToAsset(newResourcesPath, this);

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"Add Data Completed!");
    }
#endif
}

public abstract class BaseTable : ScriptableObject
{
    
}
