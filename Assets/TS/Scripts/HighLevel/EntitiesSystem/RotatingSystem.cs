using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial struct RotatingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach(var(localTransform, rotateSpeed) 
            in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateSpeed>>())
        {
            localTransform.ValueRW = localTransform.ValueRO.RotateY(rotateSpeed.ValueRO.value * SystemAPI.Time.DeltaTime);
        }
    }
}
