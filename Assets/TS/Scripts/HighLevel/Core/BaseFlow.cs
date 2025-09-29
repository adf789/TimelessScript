using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BaseFlow : ScriptableObject
{
    [SerializeField]
    protected UIType[] uis = null;

    public virtual GameState State { get; }
    public virtual async UniTask Enter()
    {
        await OpenScene();

        await OpenUI();
    }

    public virtual async UniTask Exit()
    {
        await CloseScene();

        await CloseUI();
    }

    protected async UniTask OpenScene()
    {
        Debug.Log($"Open: {State}");
        string sceneName = string.Format(StringDefine.PATH_SCENE, State);

        var scene = SceneManager.GetSceneByName(sceneName);

        if (scene.isLoaded)
            return;

        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    protected async UniTask CloseScene()
    {
        Debug.Log($"Close: {State}");
        string sceneName = string.Format(StringDefine.PATH_SCENE, State);

        var scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid())
            return;

        await SceneManager.UnloadSceneAsync(scene);
    }

    protected async UniTask OpenUI()
    {
        for (int index = 0; index < uis.Length; index++)
        {
            var ui = UIManager.Instance.GetController(uis[index]);

            await ui.Enter();
        }
    }

    protected async UniTask CloseUI()
    {
        for (int index = 0; index < uis.Length; index++)
        {
            var ui = UIManager.Instance.GetController(uis[index]);

            await ui.Exit();
        }
    }
}
