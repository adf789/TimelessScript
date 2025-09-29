using UnityEngine;

[CreateAssetMenu(fileName = "LoadingFlow", menuName = "Scriptable Objects/Flow/Loading Flow")]
public class LoadingFlow : BaseFlow
{
    public override GameState State => GameState.Loading;
}
