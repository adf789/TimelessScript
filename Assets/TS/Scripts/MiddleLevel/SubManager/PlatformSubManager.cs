
public class PlatformSubManager : SubBaseManager<PlatformSubManager>
{
#if UNITY_EDITOR
    public PlatformType Platform => PlatformType.Editor;
#elif UNITY_ANDROID
    public PlatformType Platform => PlatformType.Android;
#else
    public PlatformType Platform => PlatformType.Windows;
#endif
}
