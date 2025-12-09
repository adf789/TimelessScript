
using System;
using Unity.Mathematics;

[Serializable]
public struct LoadMapRequest
{
    public string PatternID;
    public int2 GridOffset;
    public int Priority;
    public float RequestTime;
}
