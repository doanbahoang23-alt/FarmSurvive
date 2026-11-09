using UnityEngine;

public class EnemyHealth : BaseHealth
{
    // Lưu máu gốc duy nhất 1 lần lúc khởi tạo, chống lỗi cộng dồn (Compound Scaling Bug) khi tái sử dụng Object Pool
    private float originalMaxHealth;
    private bool isOriginalHealthCached = false;

    protected virtual void Awake()
    {
        CacheOriginalHealth();
    }

    private void CacheOriginalHealth()
    {
        if (!isOriginalHealthCached)
        {
            originalMaxHealth = maxHealth > 0 ? maxHealth : 100f;
            isOriginalHealthCached = true;
        }
    }

    /// <summary>
    /// Scale máu theo đợt quái (Wave). Luôn tính toán dựa trên originalMaxHealth gốc
    /// </summary>
    public void ApplyWaveMultiplier(float healthMultiplier)
    {
        CacheOriginalHealth();

        // 1. Nhân máu tối đa theo hệ số Wave
        maxHealth = originalMaxHealth * healthMultiplier;

        // 2. Hồi đầy máu và reset trạng thái sống
        currentHealth = maxHealth;
        IsDead = false;

        NotifyHealthChanged();
    }
}

