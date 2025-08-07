using System;

[Serializable]
public struct TypeValuePair<T, V> where T : Enum where V : UnityEngine.Object
{
    #region Coding rule : Property
    public T type;
    public V value;
    #endregion Coding rule : Property

    #region Coding rule : Value
    #endregion Coding rule : Value

    #region Coding rule : Function
    #endregion Coding rule : Function
}
