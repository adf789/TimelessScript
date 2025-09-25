using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Scriptable Objects/Item Data")]
public class ItemTableData : BaseTableData
{
    [Header("Basic Info")]
    public string itemName;
    public ItemType itemType;

    [Header("Display")]
    public string icon;
    public string description;

    [Header("Stack Settings")]
    public bool isStackable = true;
    public long maxStackSize = 999;

    [Header("Value")]
    public int baseValue;

    public Item CreateItem(long count = 1)
    {
        return new Item
        {
            ID = id,
            Type = itemType,
            Count = isStackable ? count : 1
        };
    }
}