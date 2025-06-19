using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIBridge", menuName = "Scriptable Objects/UIBridge")]
public class UIBridge : ScriptableObject
{
    [Serializable]
    public struct BridgePair
    {
        public UIType uiType;
        public string typeName;


        public BridgePair(UIType uiType, string typeName)
        {
            this.uiType = uiType;
            this.typeName = typeName;
        }
    }

    public IReadOnlyList<BridgePair> Controllers { get => controllers; }

    [SerializeField]
    private List<BridgePair> controllers = new List<BridgePair>();

    private static UIBridge bridge = null;

    private const string bridgePath = "Assets/TS/ResourcesAddressable/ScriptableObjects/UIBridge.asset";

    public static UIBridge Get()
    {
        // 임시 기능
        if (bridge != null)
            return bridge;

        bridge = UnityEditor.AssetDatabase.LoadAssetAtPath<UIBridge>(bridgePath);

        return bridge;
    }

    public void Add(UIType uiType, string typeName)
    {
        if (controllers.FindIndex(p => p.uiType == uiType) == -1)
            controllers.Add(new BridgePair(uiType, typeName));

        Debug.LogError("Add " + uiType + ", " + controllers.Count + ", " + typeName + " " + StackTraceUtility.ExtractStackTrace());
    }

    public void Remove(UIType uiType)
    {
        int removeCount = controllers.RemoveAll(p => p.uiType == uiType);

        Debug.LogError("Remove " + uiType + ", " + controllers.Count + " " + StackTraceUtility.ExtractStackTrace());
    }

    public string GetTypeName(UIType uiType)
    {
        return controllers.Find(c => c.uiType == uiType).typeName;
    }
}
