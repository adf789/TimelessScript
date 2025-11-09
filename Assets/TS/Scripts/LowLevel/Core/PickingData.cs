
using System.Collections.Generic;
using UnityEngine;

public struct PickingData
{
    public Vector2 CurrentPosition { get; private set; }
    public Vector2 DeltaPosition { get; private set; }

    public PickingData(Vector2 currentPosition, Vector2 deltaPosition)
    {
        CurrentPosition = currentPosition;
        DeltaPosition = deltaPosition;
    }

    public override string ToString()
    {
        return $"Position: {CurrentPosition}, Delta: {DeltaPosition}";
    }
}
