using UnityEngine;

public abstract class BaseFlow : ScriptableObject
{
    [SerializeField]
    protected UIType[] uis = null;

    public virtual GameState State { get; }
    public virtual void Enter()
    {
        OpenUI();
    }

    public virtual void Exit()
    {
        CloseUI();
    }

    protected async void OpenUI()
    {
        for(int index = 0; index < uis.Length; index++)
        {
            var ui = UIManager.Instance.GetController(uis[index]);

            await ui.Enter();
        }
    }

    protected async void CloseUI()
    {
        for (int index = 0; index < uis.Length; index++)
        {
            var ui = UIManager.Instance.GetController(uis[index]);

            await ui.Exit();
        }
    }
}
