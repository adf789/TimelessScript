using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseController<T, V> : BaseController where T: BaseView where V : BaseModel
{
    public virtual T GetView<T>() where T : BaseView
    {
        return view as T;
    }

    public virtual V GetModel<V>() where V : BaseModel, new()
    {
        if (model == null)
            model = new V();

        return model as V;
    }
}


public class BaseController
{
    public IReadOnlyCollection<BaseController> ChildControllers { get => childControllers; }
    public virtual UIType UIType { get; }
    public virtual bool IsPopup { get; }
    public virtual string ViewPath { get => string.Format(StringDefine.PATH_LOAD_VIEW_PREFAB, UIType, (IsPopup ? "Popup" : "View")); }

    private Queue<BaseController> childControllers = null;
    private Func<BaseController, UniTask> onEventEnter = null;
    private Func<BaseController, UniTask> onEventExit = null;

    protected BaseView view = null;
    protected BaseModel model = null;

    public void CreateView(Transform parent)
    {
        if (view)
            return;

        var loadView = Resources.Load<BaseView>(ViewPath);

        view = GameObject.Instantiate(loadView, parent);
    }

    public void DeleteView()
    {
        if (!view)
            return;

        GameObject.DestroyImmediate(view.gameObject);
    }

    public void SetEventEnter(Func<BaseController, UniTask> onEvent)
    {
        onEventEnter = onEvent;
    }

    public void SetEventExit(Func<BaseController, UniTask> onEvent)
    {
        onEventExit = onEvent;
    }

    public async UniTask Enter()
    {
        await onEventEnter(this);
    }

    public async UniTask Exit()
    {
        await onEventExit(this);
    }

    public bool AddChild(BaseController controller)
    {
        if (this == controller)
            return false;

        if (childControllers == null)
        {
            if(childControllers == null)
                childControllers = new Queue<BaseController>();
        }

        childControllers.Enqueue(controller);

        return true;
    }

    public bool CheckHaveControllerInChild(BaseController controller)
    {
        if (childControllers == null)
            return false;

        if (this == controller)
            return true;

        foreach (var childController in childControllers)
        {
            if (childController == controller)
                return true;

            if (childController.CheckHaveControllerInChild(controller))
                return true;
        }

        return false;
    }
}
