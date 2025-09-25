
using System.Collections.Generic;

public class InventoryModel
{
    public int ItemCount => items.Count;

    private Dictionary<uint, Item> items = new();

    public void Add(uint itemID, long count)
    {
        var existingItem = GetItem(itemID);

        if (existingItem.IsNull)
        {
            var newItem = new Item()
            {
                ID = itemID,
                Type = ItemType.None, // 타입 추가 필요
                Count = count,
            };
            items[itemID] = newItem;
        }
        else
        {
            var updatedItem = existingItem.AddCount(count);

            items[itemID] = updatedItem;
        }
    }

    public bool RemoveItem(uint itemID, long count)
    {
        if (!items.TryGetValue(itemID, out var item))
            return false;

        if (item.Count < count)
            return false;

        var updatedItem = item;
        updatedItem.Count -= count;

        if (updatedItem.Count <= 0)
        {
            items.Remove(itemID);
        }
        else
        {
            items[itemID] = updatedItem;
        }

        return true;
    }

    public Item GetItem(uint itemID)
    {
        if (items.TryGetValue(itemID, out var itemData))
        {
            return itemData;
        }

        return default;
    }

    public bool HasItem(uint itemID, long count = 1)
    {
        var item = GetItem(itemID);
        return !item.IsNull && item.Count >= count;
    }

    public List<Item> GetAllItems()
    {
        return new List<Item>(items.Values);
    }

    public IEnumerable<Item> GetItemsByType(ItemType itemType)
    {
        foreach (var item in items.Values)
        {
            if (item.Type == itemType)
            {
                yield return item;
            }
        }
    }

    public void Clear()
    {
        items.Clear();
    }
}