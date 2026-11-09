public interface ISlowable
{
    void ApplySlow(float PercentageSlow, float duration); // (skill/wepon)
    void SetZoneSlow(float PercentageSlow, bool isInsideZone); //(zone)
}