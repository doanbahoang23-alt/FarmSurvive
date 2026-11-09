using System.Collections.Generic;
using UnityEngine;

public class Weapon : BaseDamage
{
    [Header("Cài đặt mục tiêu")]
    [SerializeField] private string targetTag = "Enemy";

    [Header("Loại công cụ khai thác")]
    [SerializeField] private ToolType weaponToolType = ToolType.None;

    public string TargetTag => targetTag;
    public ToolType WeaponToolType => weaponToolType;

    // Chống gây sát thương lặp nhiều lần trong cùng 1 lần vung / đâm vũ khí
    private readonly Dictionary<int, float> hitCooldowns = new Dictionary<int, float>();
    private const float HIT_COOLDOWN = 0.22f;

    protected virtual void Awake()
    {
        // Đảm bảo Collider2D trên vũ khí là Trigger để đâm xuyên và nhận diện va chạm mượt mà
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    public virtual void ApplyWeaponData(WeaponData data)
    {
        if (data == null) return;
        currentBaseDamage = data.BaseDamage;
        currentKnockbackForce = data.KnockbackForce;
        weaponToolType = data.ToolType;
    }

    public virtual void SetWeaponStats(float weaponDamage, float knockback, ToolType toolType)
    {
        base.SetWeaponStats(weaponDamage, knockback);
        weaponToolType = toolType;
    }

    public virtual void PerformAttack()
    {
        PlayAttackAudio();
    }

    protected void PlayAttackAudio()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.attackClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.attackClip);
        }
    }

    public virtual bool CanHitTarget(GameObject target)
    {
        if (target == null) return false;
        int id = target.GetInstanceID();
        if (hitCooldowns.TryGetValue(id, out float lastTime))
        {
            if (Time.time - lastTime < HIT_COOLDOWN) return false;
        }
        hitCooldowns[id] = Time.time;
        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.collider != null)
        {
            HandleCollision(collision.collider);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision != null && collision.collider != null)
        {
            HandleCollision(collision.collider);
        }
    }

    public virtual void HandleCollision(Collider2D collision)
    {
        if (collision == null) return;
        if (collision.transform.root == transform.root) return;

        bool isEnemy = (!string.IsNullOrEmpty(targetTag) && (collision.CompareTag(targetTag) || collision.transform.root.CompareTag(targetTag)))
            || collision.gameObject.layer == LayerMask.NameToLayer("Enemy")
            || collision.GetComponentInParent<EnemyController>() != null
            || collision.GetComponentInParent<EnemyHealth>() != null;

        ResourceNode resourceNode = collision.GetComponent<ResourceNode>() ?? collision.GetComponentInParent<ResourceNode>();
        bool isResource = resourceNode != null;

        if (!isEnemy && !isResource) return;

        GameObject targetRoot = collision.transform.root.gameObject;
        if (!CanHitTarget(targetRoot)) return;

        if (isResource)
        {
            ToolType currentTool = weaponToolType != ToolType.None
                ? weaponToolType
                : ToolManager.GetEquippedTool(transform.root.gameObject);

            if (!ToolManager.CanHarvestResource(currentTool, resourceNode.Type))
            {
                return;
            }
        }

        // Gây sát thương (TakeDamage)
        IDamageable damageable = collision.GetComponent<IDamageable>()
            ?? collision.GetComponentInParent<IDamageable>()
            ?? collision.GetComponentInChildren<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(AttackDamage());

            if (isResource && AudioManager.Instance != null)
            {
                if (resourceNode.Type == ResourceType.Rock && AudioManager.Instance.mineRockClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.mineRockClip);
                }
                else if (resourceNode.Type == ResourceType.Tree && AudioManager.Instance.chopWoodClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.chopWoodClip);
                }
            }
        }

        // Gây hiệu ứng Đẩy Lùi (Knockback)
        IKnockbackable knockbackable = collision.GetComponent<IKnockbackable>()
            ?? collision.GetComponentInParent<IKnockbackable>()
            ?? collision.GetComponentInChildren<IKnockbackable>();

        if (knockbackable != null)
        {
            Vector2 sourcePos = transform.root != null ? (Vector2)transform.root.position : (Vector2)transform.position;
            Vector2 targetPos = (Vector2)collision.bounds.center;
            Vector2 knockbackDir = (targetPos - sourcePos).normalized;
            if (knockbackDir == Vector2.zero) knockbackDir = (Vector2)transform.up;

            float force = currentKnockbackForce > 0f ? currentKnockbackForce : 15f;
            knockbackable.ApplyKnockback(knockbackDir, force);
        }
    }
}
