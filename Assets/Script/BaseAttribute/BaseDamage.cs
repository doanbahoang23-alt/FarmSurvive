using UnityEngine;

public class BaseDamage : MonoBehaviour
{
    [Header("Chỉ số sát thương cơ bản")]
    [SerializeField] protected float currentBaseDamage = 5f;
    protected float buffDamage = 0f;

    [SerializeField] protected float currentKnockbackForce = 15f;

    public float BaseDamageAmount => currentBaseDamage;
    public float KnockbackForce => currentKnockbackForce;

    public virtual void SetDamage(float damage, float knockback = 0f)
    {
        currentBaseDamage = damage;
        currentKnockbackForce = knockback;
    }

    public virtual void SetWeaponStats(float weaponDamage, float knockback)
    {
        currentBaseDamage = weaponDamage;
        currentKnockbackForce = knockback;
    }

    public virtual void SetBuffDamage(float buff)
    {
        buffDamage = buff;
    }

    public virtual float AttackDamage()
    {
        return currentBaseDamage + buffDamage;
    }
}
