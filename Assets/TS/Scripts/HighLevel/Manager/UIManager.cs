using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private List<BaseController> baseUIs = new List<BaseController>();
    private List<BaseController> openUIs = new List<BaseController>();

    public bool CheckOpendView()
    {
        return openUIs.Any(c => c.UIType < UIType.MaxView);
    }

    public BaseController GetController(UIType uiType)
    {
        UIBridge bridge = UIBridge.Get();

        if (bridge == null)
            return null;

        string typeName = bridge.GetTypeName(uiType);
        Type type = Type.GetType(typeName);
        BaseController controller = null;

        if (type != null)
            controller = Activator.CreateInstance(type) as BaseController;

        return controller;
    }

    /// <summary>
    /// Auto generate model.
    /// </summary>
    public async UniTask Enter(UIType uiType)
    {

    }

    public async UniTask Exit(UIType uiType)
    {

    }
}
