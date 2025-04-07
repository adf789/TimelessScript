using System;
using System.Collections.Generic;
using UnityEngine;
public class BaseController<T, V> : BaseController where T: BaseView where V : BaseModel
{
    protected BaseView view = null;
    protected BaseModel model = null;

    public virtual T GetView<T>() where T : BaseView
    {
        return null;
    }

    public virtual V GetModel<V>() where V : BaseModel
    {
        return null;
    }
}


public class BaseController
{
    public IReadOnlyCollection<BaseController> ChildControllers { get => childControllers; }

    private Queue<BaseController> childControllers = null;

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
