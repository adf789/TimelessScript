using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct RotateUpdateJob : IJobEntity
{
    public float deltaTime;

    public void Execute(in RotateSpeed rotateSpeed,
        ref LocalTransform localTransform)
    {
        float rotationValue = math.radians(rotateSpeed.value * deltaTime);
        quaternion yRotation = quaternion.RotateY(rotationValue);

        localTransform.Rotation = math.mul(localTransform.Rotation, yRotation);
    }
}
