using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIBridge", menuName = "ScriptableObjects/UIBridge")]
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

    private const string bridgePath = "Assets/TS/ResourcesAddressable/ScriptableObjects/UIBridge.asset";

    public static UIBridge Get()
    {
        // 임시 기능
        return UnityEditor.AssetDatabase.LoadAssetAtPath<UIBridge>(bridgePath);
    }

    public void Add(UIType uiType, string typeName)
    {
        if (controllers.FindIndex(p => p.uiType == uiType) == -1)
            controllers.Add(new BridgePair(uiType, typeName));
    }

    public void Remove(UIType uiType)
    {
        controllers.RemoveAll(p => p.uiType == uiType);
    }
}
