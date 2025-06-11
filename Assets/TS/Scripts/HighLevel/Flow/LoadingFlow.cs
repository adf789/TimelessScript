using UnityEngine;

[CreateAssetMenu(fileName = "LoadingFlow", menuName = "Scriptable Objects/Flow/Loading Flow")]
public class LoadingFlow : BaseFlow
{
    public override GameState State => GameState.Loading;


    public override void Enter()
    {
        throw new System.NotImplementedException();
    }

    public override void Exit()
    {
        throw new System.NotImplementedException();
    }
}
