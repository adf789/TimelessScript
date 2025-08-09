using UnityEngine;

public class GroundObject : MonoBehaviour
{
    [Header("Ground Settings")]
    public float bounciness = 0.3f; // 반발력 (0~1)
    public float friction = 0.8f;   // 마찰력 (0~1)
    public bool isOneWayPlatform = false; // 일방통행 플랫폼
    public GroundType groundType = GroundType.Normal;
    
    private LightweightPhysics2D physics2D;
    
    void Start()
    {
        physics2D = GetComponent<LightweightPhysics2D>();
        
        // Ground 전용 설정
        SetupAsGround();
    }
    
    void SetupAsGround()
    {
        if (physics2D != null)
        {
            // Ground는 물리적 충돌만, 트리거 아님
            physics2D.isTrigger = false;
            
            // Ground는 중력 받지 않고 고정
            physics2D.useGravity = false;
            physics2D.mass = float.MaxValue; // 무한대 질량으로 움직이지 않게
            physics2D.isStatic = true;
            
            // 초기 속도 0
            physics2D.velocity = Vector2.zero;
            
            // 드래그 없음 (고정 객체)
            physics2D.drag = 1f;
            
            // 태그 설정
            gameObject.tag = "Ground";
        }
    }
    
    // Ground와 충돌했을 때의 반응 계산
    public Vector2 CalculateCollisionResponse(Vector2 incomingVelocity, Vector2 collisionNormal)
    {
        Vector2 result = incomingVelocity;
        
        switch (groundType)
        {
            case GroundType.Normal:
                result = HandleNormalGround(incomingVelocity, collisionNormal);
                break;
            case GroundType.Bouncy:
                result = HandleBouncyGround(incomingVelocity, collisionNormal);
                break;
            case GroundType.Slippery:
                result = HandleSlipperyGround(incomingVelocity, collisionNormal);
                break;
            case GroundType.Sticky:
                result = HandleStickyGround(incomingVelocity, collisionNormal);
                break;
        }
        
        return result;
    }
    
    Vector2 HandleNormalGround(Vector2 velocity, Vector2 normal)
    {
        // 수직 성분: 반발
        Vector2 normalVelocity = Vector2.Dot(velocity, normal) * normal;
        Vector2 tangentVelocity = velocity - normalVelocity;
        
        // 수직 반발 적용
        normalVelocity *= -bounciness;
        
        // 마찰 적용
        tangentVelocity *= friction;
        
        return normalVelocity + tangentVelocity;
    }
    
    Vector2 HandleBouncyGround(Vector2 velocity, Vector2 normal)
    {
        Vector2 normalVelocity = Vector2.Dot(velocity, normal) * normal;
        return velocity - 2f * normalVelocity; // 완전 반발
    }
    
    Vector2 HandleSlipperyGround(Vector2 velocity, Vector2 normal)
    {
        Vector2 normalVelocity = Vector2.Dot(velocity, normal) * normal;
        Vector2 tangentVelocity = velocity - normalVelocity;
        
        normalVelocity *= -0.1f; // 약간의 반발
        // 마찰 거의 없음
        
        return normalVelocity + tangentVelocity;
    }
    
    Vector2 HandleStickyGround(Vector2 velocity, Vector2 normal)
    {
        Vector2 normalVelocity = Vector2.Dot(velocity, normal) * normal;
        Vector2 tangentVelocity = velocity - normalVelocity;
        
        normalVelocity *= -0.2f; // 적은 반발
        tangentVelocity *= 0.1f; // 강한 마찰
        
        return normalVelocity + tangentVelocity;
    }
}

public enum GroundType
{
    Normal,
    Bouncy,
    Slippery,
    Sticky
}