using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : BaseManager<UIManager>
{
    private List<BaseController> openControllers = new List<BaseController>();

    private Dictionary<UIType, BaseController> readyControllers = null;

    public bool CheckOpenedView()
    {
        return openControllers.Count > 0;
    }

    public bool CheckOpenedView(UIType uiType)
    {
        return openControllers.Any(c => c.UIType == uiType);
    }

    public BaseController GetController(UIType uiType)
    {
        if (readyControllers == null)
            readyControllers = new Dictionary<UIType, BaseController>();

        if (readyControllers.TryGetValue(uiType, out var controller))
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

            readyControllers.Add(uiType, newController);
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

        if (CheckOpenedView(controller.UIType))
            return;

        // 호출 대기 중인 UI 삭제
        readyControllers.Remove(controller.UIType);

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
        
    }
}
