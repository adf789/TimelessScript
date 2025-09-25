using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemTable", menuName = "Scriptable Objects/ItemTable")]
public class ItemTable : BaseTable<ItemTableData>
{
    // ItemTable의 기본 설정값들은 BaseTable의 기본값을 그대로 사용
    // 필요시 Inspector에서 idBandwidth를 다른 값으로 변경 가능
    // 예: 아이템=1000000, 퀘스트=2000000, 스킬=3000000 등
    public ItemType GetItemType(uint itemID)
    {
        var itemData = Get(itemID);
        return itemData?.itemType ?? ItemType.None;
    }

    public bool IsStackable(uint itemID)
    {
        var itemData = Get(itemID);
        return itemData?.isStackable ?? false;
    }

    public long GetMaxStackSize(uint itemID)
    {
        var itemData = Get(itemID);
        return itemData?.maxStackSize ?? 1;
    }

    public string GetItemName(uint itemID)
    {
        var itemData = Get(itemID);
        return itemData?.itemName ?? "";
    }

    public string GetItemDescription(uint itemID)
    {
        var itemData = Get(itemID);
        return itemData?.description ?? "";
    }

    public int GetBaseValue(uint itemID)
    {
        var itemData = Get(itemID);
        return itemData?.baseValue ?? 0;
    }

    public IEnumerable<ItemTableData> GetItemsByType(ItemType itemType)
    {
        foreach (var item in GetAllDatas())
        {
            if (item != null && item.itemType == itemType)
            {
                yield return item;
            }
        }
    }
}
