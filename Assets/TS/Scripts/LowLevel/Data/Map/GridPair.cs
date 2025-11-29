
using System;
using Unity.Mathematics;
using UnityEngine;

public struct GridPair : IEquatable<GridPair>
{
    private int2 _firstGrid;
    private int2 _secondGrid;

    public GridPair(int2 first, int2 second)
    {
        if (first.x < second.x)
        {
            _firstGrid = first;
            _secondGrid = second;
        }
        else if (first.x > second.x)
        {
            _firstGrid = second;
            _secondGrid = first;
        }
        else if (first.y < second.y)
        {
            _firstGrid = first;
            _secondGrid = second;
        }
        else if (first.y > second.y)
        {
            _firstGrid = second;
            _secondGrid = first;
        }
        else
        {
            Debug.LogError($"Wrong grid key: {first}");
            _firstGrid = _secondGrid = first;
        }
    }

    public bool Equals(GridPair other)
    {
        var checkFirst = _firstGrid == other._firstGrid;
        var checkSecond = _secondGrid == other._secondGrid;

        return checkFirst.x && checkFirst.y && checkSecond.x && checkSecond.y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_firstGrid, _secondGrid);
    }
}
