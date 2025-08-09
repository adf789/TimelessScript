using UnityEngine;

[System.Serializable]
public class LightweightCollider2D
{
    public LightweightPhysics2D physics;
    public Vector2 size;
    public Vector2 offset;
    public bool isTrigger;
    public Vector2 position;
    
    public LightweightCollider2D(LightweightPhysics2D physics, Vector2 size, Vector2 offset, bool isTrigger)
    {
        this.physics = physics;
        this.size = size;
        this.offset = offset;
        this.isTrigger = isTrigger;
        this.position = physics.transform.position;
    }
    
    public void UpdatePosition(Vector2 newPosition)
    {
        position = newPosition;
    }
    
    public Bounds GetBounds()
    {
        Vector2 center = position + offset;
        return new Bounds(center, size);
    }
    
    public bool Intersects(LightweightCollider2D other)
    {
        Bounds thisBounds = GetBounds();
        Bounds otherBounds = other.GetBounds();
        return thisBounds.Intersects(otherBounds);
    }
    
    public Vector2 GetSeparationVector(LightweightCollider2D other)
    {
        Bounds thisBounds = GetBounds();
        Bounds otherBounds = other.GetBounds();
        
        Vector2 separation = Vector2.zero;
        
        float overlapX = Mathf.Min(thisBounds.max.x, otherBounds.max.x) - 
                        Mathf.Max(thisBounds.min.x, otherBounds.min.x);
        float overlapY = Mathf.Min(thisBounds.max.y, otherBounds.max.y) - 
                        Mathf.Max(thisBounds.min.y, otherBounds.min.y);
        
        if (overlapX < overlapY)
        {
            // X축으로 분리
            separation.x = thisBounds.center.x < otherBounds.center.x ? -overlapX : overlapX;
        }
        else
        {
            // Y축으로 분리
            separation.y = thisBounds.center.y < otherBounds.center.y ? -overlapY : overlapY;
        }
        
        return separation;
    }
}