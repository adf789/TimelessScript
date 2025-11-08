
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(ButtonTargetAddon))]
public class ButtonTargetAddonInspector : Editor
{
    private ButtonTargetAddon inspectorTarget;

    private void OnEnable()
    {
        inspectorTarget = (ButtonTargetAddon) target;
    }

    public override void OnInspectorGUI()
    {

    }
}
