
using System.Collections.Generic;

public class InventoryModel
{
    public int ItemCount => items.Count;

    private Dictionary<uint, Item> items = new();

    public void Add(uint itemID, long count)
    {
        var item = GetItem(itemID);
        ItemType type = ItemType.None;

        // 타입 가져오는 코드 필요

        if (item.IsNull)
        {
            item = new Item()
            {
                ID = itemID,
                Type = type
            };
        }

        items[itemID] = item;
    }

    public Item GetItem(uint itemID)
    {
        if (items.TryGetValue(itemID, out var itemData))
        {
            return itemData;
        }

        return default;
    }
}