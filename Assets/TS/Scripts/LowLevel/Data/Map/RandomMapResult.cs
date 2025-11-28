
using System;
using Unity.Mathematics;

[Serializable]
public struct RandomMapResult
{
    public string PatterID;
    public int2 TopLinkPosition;
    public int2 BottomLinkPosition;

    public RandomMapResult(string id, int2 topPos, int2 bottomPos)
    {
        PatterID = id;
        TopLinkPosition = topPos;
        BottomLinkPosition = bottomPos;
    }
}
