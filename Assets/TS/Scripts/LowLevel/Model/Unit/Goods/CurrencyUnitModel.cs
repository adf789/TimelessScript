
public struct CurrencyUnitModel : IUnitModel
{
    public string Count { get; private set; }

    public void SetCount(long count)
    {
        Count = count.ToString("n0");
    }
}
