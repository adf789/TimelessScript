
using UnityEngine;

public class TestController : BaseController<TestView, TestModel>
{
    public override UIType UIType => UIType.Test;
    public override bool IsPopup => base.IsPopup;
}