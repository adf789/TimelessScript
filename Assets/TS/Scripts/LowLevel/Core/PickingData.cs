
using System.Collections.Generic;
using UnityEngine;

public struct PickingData
{
    //============================================================
    //=========    Coding rule에 맞춰서 작업 바랍니다.   =========
    //========= Coding rule region은 절대 지우지 마세요. =========
    //=========    문제 시 '김철옥'에게 문의 바랍니다.   =========
    //============================================================

    #region Coding rule : Property
    public Vector2 CurrentPosition { get; private set; }
    public Vector2 DeltaPosition { get; private set; }
    #endregion Coding rule : Property

    #region Coding rule : Value
    #endregion Coding rule : Value

    #region Coding rule : Function
    public PickingData(Vector2 currentPosition, Vector2 deltaPosition)
    {
        CurrentPosition = currentPosition;
        DeltaPosition = deltaPosition;
    }

    public override string ToString()
    {
        return $"Position: {CurrentPosition}, Delta: {DeltaPosition}";
    }
    #endregion Coding rule : Function
}
