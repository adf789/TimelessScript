using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerSubManager : SubBaseManager<PlayerSubManager>
{
    private string _playerID;

    public InventoryModel Inventory => _inventory;

    private InventoryModel _inventory = new InventoryModel();

    public void SetPlayerID(string playerID)
    {
        _playerID = playerID;
    }

    public async UniTask SaveInventory()
    {
        if (string.IsNullOrEmpty(_playerID))
        {
            Debug.LogError("Player ID is Null.");
            return;
        }

        await DatabaseSubManager.Instance.SetDocumentAsync("items", _playerID, Inventory.ConvertToSaveData());
    }

    public async UniTask LoadInventory()
    {
        var document = await DatabaseSubManager.Instance.GetDocumentAsync("items", _playerID);

        _inventory.Clear();

        if (document.Exists)
            Inventory.ConvertFromSaveData(document.ToDictionary());
    }
}
