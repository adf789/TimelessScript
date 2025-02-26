using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIBridge", menuName = "ScriptableObjects/UIBridge")]
public class UIBridge : ScriptableObject
{
    [SerializeField]
    private Dictionary<UIType, Type> bridges;

}
