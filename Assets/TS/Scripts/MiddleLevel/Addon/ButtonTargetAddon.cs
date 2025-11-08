using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ButtonTargetAddon : Graphic
{
    protected void OnValidate()
    {
        raycastTarget = true;
    }
    protected override void UpdateMaterial()
    {
    }

    protected override void UpdateGeometry()
    {
    }
}
