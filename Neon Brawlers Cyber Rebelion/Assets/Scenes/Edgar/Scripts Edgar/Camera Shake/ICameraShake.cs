
public enum ShakeIntensity
{
    Low,
    Medium,
    High
}

public interface ICameraShake
{
    bool IsShaking { get; }
    float CurrentDuration { get; }
    float RemainingDuration { get; }

    void Shake(ShakeIntensity intensity);
    void Shake(float intensity, float duration);
    void StopShake();
}