using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : BaseManager<UIManager>
{
    [SerializeField] private Canvas viewCanvas;
    [SerializeField] private Canvas frontCanvas;

    private List<BaseController> openControllers = new List<BaseController>();
    private Dictionary<UIType, BaseController> openedControllers = null;

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
        if (openedControllers == null)
            openedControllers = new Dictionary<UIType, BaseController>();

        if (openedControllers.TryGetValue(uiType, out var controller))
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

            openedControllers.Add(uiType, newController);
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

        await controller.BeforeEnterProcess();

        controller.CreateView(viewCanvas.transform);
        controller.InitializeModel();

        await controller.EnterProcess();
    }

    public async UniTask Exit(BaseController controller)
    {
        if (controller == null)
            return;

        await controller.BeforeExitProcess();

        controller.DeleteView();

        await controller.ExitProcess();

        // 오픈된 UI 캐싱 삭제
        openedControllers.Remove(controller.UIType);
    }
}
