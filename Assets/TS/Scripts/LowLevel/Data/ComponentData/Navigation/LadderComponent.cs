using Unity.Entities;
using Unity.Mathematics;

public struct LadderComponent : IComponentData
{
    // 지형 연결 정보 (직접 연결 방식)
    public Entity TopConnectedGround;
    public Entity BottomConnectedGround;
}