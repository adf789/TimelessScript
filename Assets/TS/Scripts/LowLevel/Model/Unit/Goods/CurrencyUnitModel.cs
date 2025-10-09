
public struct CurrencyUnitModel : IUnitModel
{
    public string Count { get; private set; }

    public void SetCount(long count)
    {
        Count = FormatNumber(count);
    }

    private string FormatNumber(long value)
    {
        if (value >= 1_000_000_000_000_000_000) // Quintillion (Q)
        {
            double q = value / 1_000_000_000_000_000_000.0;
            return q % 1 == 0 ? $"{q:F0}Q" : $"{q:F1}Q";
        }
        else if (value >= 1_000_000_000_000_000) // Quadrillion (q)
        {
            double quad = value / 1_000_000_000_000_000.0;
            return quad % 1 == 0 ? $"{quad:F0}q" : $"{quad:F1}q";
        }
        else if (value >= 1_000_000_000_000) // Trillion (t)
        {
            double t = value / 1_000_000_000_000.0;
            return t % 1 == 0 ? $"{t:F0}t" : $"{t:F1}t";
        }
        else if (value >= 1_000_000_000) // Billion (b)
        {
            double b = value / 1_000_000_000.0;
            return b % 1 == 0 ? $"{b:F0}b" : $"{b:F1}b";
        }
        else if (value >= 1_000_000) // Million (m)
        {
            double m = value / 1_000_000.0;
            return m % 1 == 0 ? $"{m:F0}m" : $"{m:F1}m";
        }
        else if (value >= 1_000) // Thousand (k)
        {
            double k = value / 1_000.0;
            return k % 1 == 0 ? $"{k:F0}k" : $"{k:F1}k";
        }
        else
        {
            return value.ToString("n0");
        }
    }
}
