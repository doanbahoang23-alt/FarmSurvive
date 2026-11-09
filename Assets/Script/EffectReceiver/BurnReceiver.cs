using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BurnReceiver : MonoBehaviour, IBurnable
{
    private SpriteRenderer[] spriteRenderers;
    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private IDamageable damageable;
    private bool isBurning = false;
    private Coroutine burnCoroutine;
    private float currentDamagePerSecond = 5f;
    private float remainingBurnDuration = 0f;

    public bool IsBurning => isBurning;

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
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }

        isBurning = false;
        remainingBurnDuration = 0f;
        RestoreOriginalColors();
    }

    private void ResolveDependencies()
    {
        if (damageable == null)
        {
            damageable = GetComponent<IDamageable>() 
                      ?? GetComponentInParent<IDamageable>() 
                      ?? GetComponentInChildren<IDamageable>();
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

    public void ApplyBurn(float damePerSecond, float duration)
    {
        ResolveDependencies();

        currentDamagePerSecond = damePerSecond;
        remainingBurnDuration = Mathf.Max(remainingBurnDuration, duration);

        // Nếu chưa cháy hoặc coroutine chưa chạy thì mới khởi động BurnRoutine
        // Tránh tình trạng OnTriggerStay2D gọi 50 lần/giây làm hủy và tạo lại coroutine liên tục gây nuke sát thương dồn dập
        if (!isBurning || burnCoroutine == null)
        {
            burnCoroutine = StartCoroutine(BurnRoutine());
        }
    }

    private IEnumerator BurnRoutine()
    {
        isBurning = true;

        while (remainingBurnDuration > 0f)
        {
            if (damageable != null)
            {
                damageable.TakeDamage(currentDamagePerSecond);
            }

            StartCoroutine(FlickerBurnColor());

            yield return new WaitForSeconds(1f);
            remainingBurnDuration -= 1f;
        }

        RestoreOriginalColors();
        isBurning = false;
        burnCoroutine = null;
        remainingBurnDuration = 0f;
    }

    private IEnumerator FlickerBurnColor()
    {
        // Hiệu ứng nhấp nháy chuyển màu đỏ cam rực lửa
        SetSpriteColor(new Color(1f, 0.28f, 0.1f, 1f));
        yield return new WaitForSeconds(0.4f);

        if (isBurning)
        {
            SetSpriteColor(new Color(1f, 0.65f, 0.45f, 1f));
        }
        else
        {
            RestoreOriginalColors();
        }
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
