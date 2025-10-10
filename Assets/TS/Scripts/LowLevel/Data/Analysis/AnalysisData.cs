
public struct AnalysisData
{
    public float CurrentFPS { get; private set; }
    public float AverageFPS { get; private set; }
    public int SpawnCount { get; private set; }
    private const float fpsSmoothingFactor = 0.1f;

    public void SetFPS(float fps)
    {
        CurrentFPS = fps;
        AverageFPS = AverageFPS == 0 ? fps : AverageFPS * (1f - fpsSmoothingFactor) + fps * fpsSmoothingFactor;
    }

    public void SetSpawnCount(int count)
    {
        SpawnCount = count;
    }
}
