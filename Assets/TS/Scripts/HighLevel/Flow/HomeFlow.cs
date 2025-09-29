using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "HomeFlow", menuName = "Scriptable Objects/Flow/Town Flow")]
public class TownFlow : BaseFlow
{
    public override GameState State => GameState.Town;
}
