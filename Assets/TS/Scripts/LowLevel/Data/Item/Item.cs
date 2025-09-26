public struct Item
{
    public bool IsNull => ID == 0;
    public uint ID;
    public ItemType Type;
    public long Count;

    public Item(uint id, ItemType type, long count = 0)
    {
        ID = id;
        Type = type;
        Count = count;
    }

    public Item(ItemTableData data, long count = 0)
    {
        ID = data.ID;
        Type = data.itemType;
        Count = 0;
    }

    public Item SetCount(long count)
    {
        Count = count;

        return this;
    }

    public Item AddCount(long count)
    {
        if (Count + count > long.MaxValue)
            Count = long.MaxValue;
        else
            Count += count;

        return this;
    }
}
