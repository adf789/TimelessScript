using UnityEngine;

public class PlayerSubManager : SubBaseManager<PlayerSubManager>
{
    public InventoryModel Inventory => inventory;

    private InventoryModel inventory = new InventoryModel();
}
