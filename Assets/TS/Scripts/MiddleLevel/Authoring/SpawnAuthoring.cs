
using UnityEngine;
using Unity.Entities;

public class SpawnAuthoring : MonoBehaviour
{
    
    private class Baker : Baker<SpawnAuthoring>
    {
        public override void Bake(SpawnAuthoring authoring)
        {

        }

    }
}