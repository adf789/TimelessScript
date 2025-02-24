using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIBridge", menuName = "ScriptableObjects/UIBridge")]
public class UIBridge : ScriptableObject
{
    [System.Serializable]
    public struct UIBridgeData
    {
        public UIType uiType;
        public BaseController controller;
    }

    [SerializeField]
    private SerializableDictionary<UIType, BaseController> bridges;
}
