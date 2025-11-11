using System;
using UnityEngine;

[Serializable]
public struct GroundEntry
{
    [SerializeField] private TSGroundAuthoring _ground;
    [SerializeField] private Vector2Int _min;
    [SerializeField] private Vector2Int _max;

    public readonly TSGroundAuthoring Ground => _ground;
    public readonly Vector2Int Min => _min;
    public readonly Vector2Int Max => _max;

    public readonly bool ContainsGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= _min.x && gridPos.x <= _max.x &&
               gridPos.y >= _min.y && gridPos.y <= _max.y;
    }
}
