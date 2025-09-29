using UnityEngine;

public class IntroView : BaseView<IntroViewModel>
{
    public void OnClickNext()
    {
        Model.OnEventNext?.Invoke();
    }
}
