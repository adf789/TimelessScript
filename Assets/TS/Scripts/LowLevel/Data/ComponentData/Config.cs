using Unity.Entities;
using UnityEngine;

public struct Config : IComponentData
{
    public Entity prefab;
    public float spawnRadius;
    public int spawnCount;
    public uint randomSeed;
}
