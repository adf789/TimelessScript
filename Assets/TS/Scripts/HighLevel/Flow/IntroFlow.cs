using UnityEngine;

[CreateAssetMenu(fileName = "IntroFlow", menuName = "TS/Flow/Intro Flow")]
public class IntroFlow : BaseFlow
{
    public override GameState State => GameState.Intro;
}
