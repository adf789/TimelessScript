using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIBridge", menuName = "ScriptableObjects/UIBridge")]
public class UIBridge : ScriptableObject
{
    public IReadOnlyDictionary<UIType, BaseController> Controllers { get => controllers; }

    [SerializeField]
    private Dictionary<UIType, BaseController> controllers = new Dictionary<UIType, BaseController>();

    public void Add(UIType uiType, BaseController controller)
    {
        if(!controllers.ContainsKey(uiType))
            controllers[uiType] = controller;
    }

    public void Remove(UIType uiType)
    {
        controllers.Remove(uiType);
    }
}
