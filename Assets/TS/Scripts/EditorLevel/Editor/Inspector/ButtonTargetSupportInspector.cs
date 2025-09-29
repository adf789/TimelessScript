
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(ButtonTargetSupport))]
public class ButtonTargetSupportInspector : Editor
{
    private ButtonTargetSupport inspectorTarget;

    private void OnEnable()
    {
        inspectorTarget = (ButtonTargetSupport) target;
    }

    public override void OnInspectorGUI()
    {

    }
}
