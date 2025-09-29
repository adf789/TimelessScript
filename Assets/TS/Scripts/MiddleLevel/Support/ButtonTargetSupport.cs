using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ButtonTargetSupport : Graphic
{
    protected override void OnValidate()
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
