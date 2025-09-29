
using UnityEngine;

public class LobbyView : BaseView<LobbyViewModel>
{
    public void OnClickShowInventory()
    {
        Model.OnEventShowInventory();
    }
}
