using UnityEngine;
using System.Collections.Generic;
using System;

public class LightweightPhysics2D : MonoBehaviour
{
    [Header("Physics")]
    public Vector2 velocity;
    public Vector2 gravity = new Vector2(0, -9.81f);
    public float mass = 1f;
    public float drag = 0.98f;
    public bool useGravity = true;
    public bool isGrounded = false;
    public bool isStatic = false;

    [Header("Collision")]
    public Vector2 colliderSize = Vector2.one;
    public Vector2 colliderOffset = Vector2.zero;
    public bool isTrigger = false;
    public LayerMask collisionLayers = -1;

    // 이벤트
    public event Action<LightweightCollider2D> OnTriggerEnter;
    public event Action<LightweightCollider2D> OnTriggerStay;
    public event Action<LightweightCollider2D> OnTriggerExit;

    private Vector2 position;
    private new LightweightCollider2D collider2D;
    private HashSet<LightweightCollider2D> currentTriggers = new HashSet<LightweightCollider2D>();

    // 정적 관리
    private static List<LightweightPhysics2D> allBodies = new List<LightweightPhysics2D>();
    private static LightweightCollisionSystem collisionSystem;

    private bool isGround = false;

    void Start()
    {
        position = transform.position;
        allBodies.Add(this);

        // 콜라이더 초기화
        collider2D = new LightweightCollider2D(this, colliderSize, colliderOffset, isTrigger);

        // 충돌 시스템 초기화
        if (collisionSystem == null)
        {
            GameObject systemObj = new GameObject("LightweightCollisionSystem");
            collisionSystem = systemObj.AddComponent<LightweightCollisionSystem>();
        }

        collisionSystem.RegisterCollider(collider2D);
    }

    void OnDestroy()
    {
        allBodies.Remove(this);
        if (collisionSystem != null)
        {
            collisionSystem.UnregisterCollider(collider2D);
        }
    }

    void FixedUpdate()
    {
        if (isStatic)
            return;
            
        Vector2 previousPosition = position;

        // 물리 계산
        if (useGravity && !isGrounded)
        {
            velocity += gravity * Time.fixedDeltaTime;
        }

        velocity *= drag;
        position += velocity * Time.fixedDeltaTime;

        // 충돌 검사
        HandleCollisions(previousPosition);

        // 트리거 검사
        HandleTriggers();

        // 위치 적용
        transform.position = position;
        collider2D.UpdatePosition(position);
    }

    void HandleCollisions(Vector2 previousPosition)
    {
        if (isTrigger) return;

        var collisions = collisionSystem.CheckCollisions(collider2D);
        isGrounded = false;

        foreach (var collision in collisions)
        {
            if (collision.isTrigger) continue;

            // Ground 객체와의 충돌 처리
            GroundObject ground = collision.physics.GetComponent<GroundObject>();

            if (ground != null)
            {
                HandleGroundCollision(collision, ground);
            }
            else
            {
                // 일반 객체와의 충돌
                HandleNormalCollision(collision);
            }
        }
    }

    void HandleTriggers()
    {
        var allColliders = collisionSystem.CheckTriggers(collider2D);
        var newTriggers = new HashSet<LightweightCollider2D>();

        foreach (var triggerCollider in allColliders)
        {
            if (triggerCollider.isTrigger)
            {
                newTriggers.Add(triggerCollider);

                // 트리거 진입
                if (!currentTriggers.Contains(triggerCollider))
                {
                    OnTriggerEnter?.Invoke(triggerCollider);
                    triggerCollider.physics.OnTriggerEnter?.Invoke(collider2D);
                }
                else
                {
                    // 트리거 지속
                    OnTriggerStay?.Invoke(triggerCollider);
                    triggerCollider.physics.OnTriggerStay?.Invoke(collider2D);
                }
            }
        }

        // 트리거 종료 검사
        foreach (var oldTrigger in currentTriggers)
        {
            if (!newTriggers.Contains(oldTrigger))
            {
                OnTriggerExit?.Invoke(oldTrigger);
                oldTrigger.physics.OnTriggerExit?.Invoke(collider2D);
            }
        }

        currentTriggers = newTriggers;
    }

    void HandleGroundCollision(LightweightCollider2D groundCollider, GroundObject ground)
    {
        Vector2 separation = groundCollider.GetSeparationVector(collider2D);

        // 일방통행 플랫폼 처리
        if (ground.isOneWayPlatform)
        {
            // 아래에서 위로 올라올 때만 충돌
            if (velocity.y <= 0 && separation.y > 0)
            {
                position.y += separation.y;
                velocity = ground.CalculateCollisionResponse(velocity, Vector2.up);
                isGrounded = true;
            }
        }
        else
        {
            // 일반 지면 충돌
            position += separation;

            // 충돌 노멀 계산
            Vector2 normal = separation.normalized;

            // Ground의 반응 적용
            velocity = ground.CalculateCollisionResponse(velocity, normal);

            // 바닥 판정 (위쪽으로 분리된 경우)
            if (separation.y > 0)
            {
                isGrounded = true;
            }
        }
    }

    void HandleNormalCollision(LightweightCollider2D collision)
    {
        // 기존 충돌 처리 로직
        Vector2 separation = collision.GetSeparationVector(collider2D);
        position += separation;

        if (Mathf.Abs(separation.y) > Mathf.Abs(separation.x))
        {
            velocity.y = -velocity.y * 0.5f;
            if (separation.y > 0) isGrounded = true;
        }
        else
        {
            velocity.x = -velocity.x * 0.5f;
        }
    }

    public void AddForce(Vector2 force)
    {
        velocity += force / mass;
    }

    public Bounds GetBounds()
    {
        return collider2D.GetBounds();
    }

    // 기즈모 그리기
    void OnDrawGizmos()
    {
        Vector2 center = (Vector2)transform.position + colliderOffset;

        if (isTrigger)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, colliderSize);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, colliderSize);
        }
    }
}