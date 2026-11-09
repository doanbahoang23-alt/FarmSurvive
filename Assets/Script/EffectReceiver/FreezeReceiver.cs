using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiếp nhận hiệu ứng Đóng Băng (Freeze).
/// Gây sát thương lạnh, chuyển màu xanh băng tuyết trên Sprite,
/// đóng băng cử động (Animator, di chuyển Rigidbody2D).
/// </summary>
public class FreezeReceiver : MonoBehaviour, IFreezable
{
    private SpriteRenderer[] spriteRenderers;
    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private BaseSpeed baseSpeed;
    private IDamageable damageable;
    private Animator animator;
    private Rigidbody2D rb;

    private bool isFrozen = false;
    private Coroutine freezeCoroutine;

    public bool IsFrozen => isFrozen;

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
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        isFrozen = false;
        if (baseSpeed != null) baseSpeed.SetSlowMultiplier(1f);
        if (animator != null) animator.speed = 1f;
        RestoreOriginalColors();
    }

    private void ResolveDependencies()
    {
        if (baseSpeed == null)
        {
            baseSpeed = GetComponent<BaseSpeed>() 
                     ?? GetComponentInParent<BaseSpeed>() 
                     ?? GetComponentInChildren<BaseSpeed>();
        }

        if (damageable == null)
        {
            damageable = GetComponent<IDamageable>() 
                      ?? GetComponentInParent<IDamageable>() 
                      ?? GetComponentInChildren<IDamageable>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers)
            {
                if (sr != null && !originalColors.ContainsKey(sr))
                {
                    originalColors[sr] = sr.color;
                }
            }
        }
    }

    public void ApplyFreeze(float stunDuration, float frostDuration, float damagePerSec)
    {
        ResolveDependencies();

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }
        freezeCoroutine = StartCoroutine(FreezeRoutine(stunDuration, frostDuration, damagePerSec));
    }

    private IEnumerator FreezeRoutine(float stunDuration, float frostDuration, float damagePerSec)
    {
        isFrozen = true;
        float timer = 0f;

        // Giai đoạn 1: Đóng băng hoàn toàn (xanh băng tuyết, tốc độ = 0, dừng animation)
        SetSpriteColor(new Color(0.2f, 0.65f, 1f, 1f));
        if (baseSpeed != null) baseSpeed.SetSlowMultiplier(0f);
        if (animator != null) animator.speed = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        while (timer < stunDuration)
        {
            if (damageable != null) damageable.TakeDamage(damagePerSec);
            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        // Giai đoạn 2: Băng tan dần (giảm 50% tốc độ di chuyển và cử động)
        isFrozen = false;
        if (baseSpeed != null) baseSpeed.SetSlowMultiplier(0.5f);
        if (animator != null) animator.speed = 0.6f;

        while (timer < frostDuration)
        {
            if (damageable != null) damageable.TakeDamage(damagePerSec);
            StartCoroutine(FlickerIceColor());

            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        // Khôi phục hoàn toàn
        if (baseSpeed != null) baseSpeed.SetSlowMultiplier(1f);
        if (animator != null) animator.speed = 1f;
        RestoreOriginalColors();

        isFrozen = false;
        freezeCoroutine = null;
    }

    private IEnumerator FlickerIceColor()
    {
        SetSpriteColor(new Color(0.35f, 0.75f, 1f, 1f));
        yield return new WaitForSeconds(0.35f);
        SetSpriteColor(new Color(0.7f, 0.9f, 1f, 1f));
    }

    private void SetSpriteColor(Color c)
    {
        if (spriteRenderers == null) return;
        foreach (var sr in spriteRenderers)
        {
            if (sr != null) sr.color = c;
        }
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers == null) return;
        foreach (var sr in spriteRenderers)
        {
            if (sr != null && originalColors.TryGetValue(sr, out Color orig))
            {
                sr.color = orig;
            }
        }
    }
}
