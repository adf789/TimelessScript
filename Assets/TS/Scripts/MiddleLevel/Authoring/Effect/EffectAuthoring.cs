
using UnityEngine;
using Unity.Entities;

public class EffectAuthoring : MonoBehaviour
{
    
    private class Baker : Baker<EffectAuthoring>
    {
        public override void Bake(EffectAuthoring authoring)
        {

        }

    }
}
