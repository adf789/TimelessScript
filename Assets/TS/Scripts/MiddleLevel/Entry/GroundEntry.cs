using System;
using UnityEngine;

[Serializable]
public struct GroundEntry
{
    [SerializeField] private TSGroundAuthoring _ground;
    [SerializeField] private Vector2Int _min;
    [SerializeField] private Vector2Int _max;
}
