using UnityEngine;

[CreateAssetMenu(fileName = "HomeFlow", menuName = "Scriptable Objects/Flow/Home Flow")]
public class HomeFlow : BaseFlow
{
    public override GameState State => GameState.Home;


    public override void Enter()
    {
        throw new System.NotImplementedException();
    }

    public override void Exit()
    {
        throw new System.NotImplementedException();
    }
}
