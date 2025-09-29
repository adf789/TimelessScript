using UnityEngine;

[CreateAssetMenu(fileName = "IntroFlow", menuName = "Scriptable Objects/Flow/Intro Flow")]
public class IntroFlow : BaseFlow
{
    public override GameState State => GameState.Intro;
}
