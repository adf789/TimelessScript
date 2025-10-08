using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseController<T, V> : BaseController where T : BaseView where V : BaseModel
{
    private T view = null;
    private V model = null;

    public bool ViewIsNull => view == null;

    public T GetView()
    {
        return view;
    }

    public V GetModel()
    {
        if (model == null)
            model = Activator.CreateInstance<V>();

        return model;
    }

    public override void CreateView(Transform parent)
    {
        if (view)
            return;

        var loadView = Resources.Load<T>(ViewPath);

        if (loadView == null)
        {
            Debug.LogError($"Not found view: {ViewPath}");
            return;
        }

        view = GameObject.Instantiate(loadView, parent);
    }

    public override void DeleteView()
    {
        if (!view)
            return;

        GameObject.DestroyImmediate(view.gameObject);
    }

    public override void InitializeModel()
    {
        if (view)
            view.SetModel(GetModel());
    }
}


public class BaseController
{
    public IReadOnlyCollection<BaseController> ChildControllers { get => childControllers; }
    public virtual UIType UIType { get; }
    public virtual bool IsPopup { get; }
    public virtual string ViewPath { get => string.Format(StringDefine.PATH_LOAD_VIEW_PREFAB, (IsPopup ? "Popup" : "View"), UIType); }

    private Queue<BaseController> childControllers = null;
    private Action<BaseController> onEventEnter = null;
    private Action<BaseController> onEventExit = null;

    public virtual void CreateView(Transform parent)
    {

    }

    public virtual void DeleteView()
    {

    }

    public void SetEventEnter(Action<BaseController> onEvent)
    {
        onEventEnter = onEvent;
    }

    public void SetEventExit(Action<BaseController> onEvent)
    {
        onEventExit = onEvent;
    }

    public void Enter()
    {
        onEventEnter(this);
    }

    public void Exit()
    {
        onEventExit(this);
    }

    public virtual void InitializeModel()
    {

    }

    public virtual void BeforeEnterProcess()
    {

    }

    public virtual void EnterProcess()
    {

    }

    public virtual void BeforeExitProcess()
    {

    }

    public virtual void ExitProcess()
    {

    }

    public bool AddChild(BaseController controller)
    {
        if (this == controller)
            return false;

        if (childControllers == null)
        {
            if (childControllers == null)
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
