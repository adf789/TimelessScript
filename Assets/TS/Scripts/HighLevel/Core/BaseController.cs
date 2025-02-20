using System.Collections.Generic;
using UnityEngine;

public class BaseController
{
    public IReadOnlyCollection<BaseController> ChildControllers { get => childControllers; }

    private Queue<BaseController> childControllers = null;

    protected BaseView view = null;
    protected BaseModel model = null;

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
