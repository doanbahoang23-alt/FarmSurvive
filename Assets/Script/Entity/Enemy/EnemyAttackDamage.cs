using UnityEngine;

public class EnemyAttackDamage : BaseDamage
{
    // Lưu sát thương gốc duy nhất 1 lần lúc khởi tạo, chống lỗi cộng dồn khi tái sử dụng Object Pool
    private float originalDamage;
    private bool isOriginalDamageCached = false;

    private void Awake()
    {
        CacheOriginalDamage();
    }

    private void CacheOriginalDamage()
    {
        if (!isOriginalDamageCached)
        {
            originalDamage = currentBaseDamage > 0 ? currentBaseDamage : 5f;
            isOriginalDamageCached = true;
        }
    }

    /// <summary>
    /// Scale sát thương đánh ra theo đợt quái (Wave). Luôn tính toán dựa trên originalDamage gốc
    /// </summary>
    public void ApplyDamageMultiplier(float damageMultiplier)
    {
        CacheOriginalDamage();
        currentBaseDamage = originalDamage * damageMultiplier;
    }
}

