using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private List<BaseController> baseUIs = new List<BaseController>();
    private List<BaseController> openUIs = new List<BaseController>();

    private Dictionary<UIType, BaseController> readyUIs = null;

    public bool CheckOpenedView()
    {
        return openUIs.Count > 0;
    }

    public BaseController GetController(UIType uiType)
    {
        if (readyUIs == null)
            readyUIs = new Dictionary<UIType, BaseController>();

        if (readyUIs.TryGetValue(uiType, out var controller))
            return controller;

        string typeName = string.Format(StringDefine.DEFINE_CONTROLLER_TYPE_NAME, uiType);
        Type type = Type.GetType(typeName);
        BaseController newController = null;

        if (type != null)
            newController = Activator.CreateInstance(type) as BaseController;

        if (newController != null)
        {
            newController.SetEventEnter(Enter);
            newController.SetEventExit(Exit);

            readyUIs.Add(uiType, newController);
        }

        return newController;
    }

    /// <summary>
    /// Auto generate model.
    /// </summary>
    public async UniTask Enter(BaseController controller)
    {
        if (controller == null)
            return;

        controller.CreateView(transform);
    }

    public async UniTask Exit(BaseController controller)
    {
        if (controller == null)
            return;

        controller.DeleteView();
    }

    [ContextMenu("Test")]
    public void Test()
    {
        var controller = GetController(UIType.Test);

        controller.Enter().Forget();
    }

    [ContextMenu("Test1")]
    public void Test1()
    {
        var controller = GetController(UIType.Test);

        controller.Exit().Forget();
    }
}
