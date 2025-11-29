using System;

[Serializable]
public struct MapDto
{
    public int MapCount => MapGrids != null ? MapGrids.Length : 0;
    public MapGridDto[] MapGrids;
}
