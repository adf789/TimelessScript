using UnityEngine;
using System.Collections.Generic;

public class LightweightCollisionSystem : MonoBehaviour
{
    private List<LightweightCollider2D> allColliders = new List<LightweightCollider2D>();
    
    [Header("Optimization")]
    public bool useSpacialHashing = true;
    public float cellSize = 5f;
    
    private Dictionary<Vector2Int, List<LightweightCollider2D>> spatialHash = 
        new Dictionary<Vector2Int, List<LightweightCollider2D>>();
    
    public void RegisterCollider(LightweightCollider2D collider)
    {
        allColliders.Add(collider);
    }
    
    public void UnregisterCollider(LightweightCollider2D collider)
    {
        allColliders.Remove(collider);
    }
    
    public List<LightweightCollider2D> CheckCollisions(LightweightCollider2D collider)
    {
        List<LightweightCollider2D> collisions = new List<LightweightCollider2D>();
        
        if (useSpacialHashing)
        {
            UpdateSpatialHash();
            var nearbyColliders = GetNearbyColliders(collider);
            
            foreach (var other in nearbyColliders)
            {
                if (other != collider && !other.isTrigger && collider.Intersects(other))
                {
                    collisions.Add(other);
                }
            }
        }
        else
        {
            foreach (var other in allColliders)
            {
                if (other != collider && !other.isTrigger && collider.Intersects(other))
                {
                    collisions.Add(other);
                }
            }
        }
        
        return collisions;
    }
    
    public List<LightweightCollider2D> CheckTriggers(LightweightCollider2D collider)
    {
        List<LightweightCollider2D> triggers = new List<LightweightCollider2D>();
        
        if (useSpacialHashing)
        {
            var nearbyColliders = GetNearbyColliders(collider);
            
            foreach (var other in nearbyColliders)
            {
                if (other != collider && collider.Intersects(other))
                {
                    triggers.Add(other);
                }
            }
        }
        else
        {
            foreach (var other in allColliders)
            {
                if (other != collider && collider.Intersects(other))
                {
                    triggers.Add(other);
                }
            }
        }
        
        return triggers;
    }
    
    void UpdateSpatialHash()
    {
        spatialHash.Clear();
        
        foreach (var collider in allColliders)
        {
            Vector2Int cellPos = WorldToCell(collider.position);
            
            if (!spatialHash.ContainsKey(cellPos))
            {
                spatialHash[cellPos] = new List<LightweightCollider2D>();
            }
            
            spatialHash[cellPos].Add(collider);
        }
    }
    
    List<LightweightCollider2D> GetNearbyColliders(LightweightCollider2D collider)
    {
        List<LightweightCollider2D> nearby = new List<LightweightCollider2D>();
        Vector2Int centerCell = WorldToCell(collider.position);
        
        // 주변 9개 셀 검사
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkCell = centerCell + new Vector2Int(x, y);
                if (spatialHash.ContainsKey(checkCell))
                {
                    nearby.AddRange(spatialHash[checkCell]);
                }
            }
        }
        
        return nearby;
    }
    
    Vector2Int WorldToCell(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.y / cellSize)
        );
    }
}