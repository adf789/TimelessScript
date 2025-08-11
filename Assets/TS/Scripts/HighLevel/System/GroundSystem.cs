using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct GroundSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Ground 충돌 응답 처리
        var groundCollisionJob = new GroundCollisionJob();
        state.Dependency = groundCollisionJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct GroundCollisionJob : IJobEntity
{
    public void Execute(ref GroundCollisionData collisionData, in GroundComponent ground)
    {
        if (!collisionData.hasCollision)
            return;
            
        float2 result = collisionData.incomingVelocity;
        
        switch (ground.groundType)
        {
            case GroundType.Normal:
                result = HandleNormalGround(collisionData.incomingVelocity, collisionData.collisionNormal, ground);
                break;
            case GroundType.Bouncy:
                result = HandleBouncyGround(collisionData.incomingVelocity, collisionData.collisionNormal);
                break;
            case GroundType.Slippery:
                result = HandleSlipperyGround(collisionData.incomingVelocity, collisionData.collisionNormal);
                break;
            case GroundType.Sticky:
                result = HandleStickyGround(collisionData.incomingVelocity, collisionData.collisionNormal);
                break;
        }
        
        collisionData.responseVelocity = result;
        collisionData.hasCollision = false; // 처리 완료 플래그 리셋
    }
    
    [BurstCompile]
    private float2 HandleNormalGround(float2 velocity, float2 normal, GroundComponent ground)
    {
        // 수직 성분: 반발
        float normalDot = math.dot(velocity, normal);
        float2 normalVelocity = normalDot * normal;
        float2 tangentVelocity = velocity - normalVelocity;
        
        // 수직 반발 적용
        normalVelocity *= -ground.bounciness;
        
        // 마찰 적용
        tangentVelocity *= ground.friction;
        
        return normalVelocity + tangentVelocity;
    }
    
    [BurstCompile]
    private float2 HandleBouncyGround(float2 velocity, float2 normal)
    {
        float normalDot = math.dot(velocity, normal);
        float2 normalVelocity = normalDot * normal;
        return velocity - 2f * normalVelocity; // 완전 반발
    }
    
    [BurstCompile]
    private float2 HandleSlipperyGround(float2 velocity, float2 normal)
    {
        float normalDot = math.dot(velocity, normal);
        float2 normalVelocity = normalDot * normal;
        float2 tangentVelocity = velocity - normalVelocity;
        
        normalVelocity *= -0.1f; // 약간의 반발
        // 마찰 거의 없음
        
        return normalVelocity + tangentVelocity;
    }
    
    [BurstCompile]
    private float2 HandleStickyGround(float2 velocity, float2 normal)
    {
        float normalDot = math.dot(velocity, normal);
        float2 normalVelocity = normalDot * normal;
        float2 tangentVelocity = velocity - normalVelocity;
        
        normalVelocity *= -0.2f; // 적은 반발
        tangentVelocity *= 0.1f; // 강한 마찰
        
        return normalVelocity + tangentVelocity;
    }
}