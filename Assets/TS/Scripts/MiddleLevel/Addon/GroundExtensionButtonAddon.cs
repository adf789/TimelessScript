
using Unity.Mathematics;
using UnityEngine;

public class GroundExtensionButtonAddon : MonoBehaviour
{
    private int2 _currentGrid;
    private System.Action<int2> _onEventExtension;

    public void SetEventExtension(System.Action<int2> onEvent)
    {
        _onEventExtension = onEvent;
    }

    public void SetGrid(int2 grid)
    {
        _currentGrid = grid;
    }

    public void OnClickExtension()
    {
        _onEventExtension(_currentGrid);
    }
}
