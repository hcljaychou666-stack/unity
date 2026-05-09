using UnityEngine;

public static class RuntimePerformanceSettings
{
    private const int TargetFrameRate = 60;
    private const float TargetFixedDeltaTime = 1f / TargetFrameRate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
        Time.fixedDeltaTime = TargetFixedDeltaTime;
        Time.maximumDeltaTime = 0.1f;
    }
}
