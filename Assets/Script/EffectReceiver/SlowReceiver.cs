using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiếp nhận hiệu ứng Làm Chậm (Slow).
/// Giảm tốc độ di chuyển theo phần trăm trong khoảng thời gian hoặc theo khu vực.
/// </summary>
public class SlowReceiver : MonoBehaviour, ISlowable
{
    private BaseSpeed baseSpeed;
    private Coroutine slowCoroutine;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
    }

    private void OnDisable()
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            slowCoroutine = null;
        }

        if (baseSpeed != null)
        {
            baseSpeed.SetSlowMultiplier(1f);
        }
    }

    private void ResolveDependencies()
    {
        if (baseSpeed == null)
        {
            baseSpeed = GetComponent<BaseSpeed>() 
                     ?? GetComponentInParent<BaseSpeed>() 
                     ?? GetComponentInChildren<BaseSpeed>();
        }
    }

    public void ApplySlow(float percentageSlow, float duration)
    {
        ResolveDependencies();
        if (baseSpeed == null) return;

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine = StartCoroutine(SlowRoutine(percentageSlow, duration));
    }

    private IEnumerator SlowRoutine(float percentageSlow, float duration)
    {
        float speedLeft = Mathf.Clamp01(1f - percentageSlow);
        baseSpeed.SetSlowMultiplier(speedLeft);

        yield return new WaitForSeconds(duration);

        baseSpeed.SetSlowMultiplier(1f);
        slowCoroutine = null;
    }

    public void SetZoneSlow(float percentageSlow, bool isInsideZone)
    {
        ResolveDependencies();
        if (baseSpeed == null) return;

        if (isInsideZone)
        {
            float speedLeft = Mathf.Clamp01(1f - percentageSlow);
            baseSpeed.SetSlowMultiplier(speedLeft);
        }
        else
        {
            baseSpeed.SetSlowMultiplier(1f);
        }
    }
}
