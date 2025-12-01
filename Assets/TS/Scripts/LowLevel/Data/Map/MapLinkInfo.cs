
using System;

[Serializable]
public struct MapLinkInfo : IEquatable<MapLinkInfo>
{
    public long Left;
    public long Right;
    public int UpMin;
    public int UpMax;
    public int UpY;
    public int DownMin;
    public int DownMax;
    public int DownY;

    public long GetHorizontal(FourDirection dir)
    {
        if (dir == FourDirection.Left)
            return Left;
        else if (dir == FourDirection.Right)
            return Right;
        else
            return 0;
    }

    public (int min, int max) GetVertical(FourDirection dir)
    {
        if (dir == FourDirection.Up)
            return (UpMin, UpMax);
        else if (dir == FourDirection.Down)
            return (DownMin, DownMax);
        else
            return (0, 0);
    }

    public int GetVerticalY(FourDirection dir)
    {
        if (dir == FourDirection.Up)
            return UpY;
        else if (dir == FourDirection.Down)
            return DownY;
        else
            return 0;
    }

    public bool Equals(MapLinkInfo other)
    {
        return Left == other.Left
        && Right == other.Right
        && UpMin == other.UpMin
        && UpMax == other.UpMax
        && UpY == other.UpY
        && DownMin == other.DownMin
        && DownMax == other.DownMax
        && DownY == other.DownY;
    }
}
