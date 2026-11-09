using UnityEngine;

public class MeleeWeapon : Weapon
{
    [Header("Hitbox Cận Chiến (Melee Settings)")]
    [Tooltip("Điểm xuất phát quét đòn đánh (đầu mũi giáo, lưỡi kiếm...). Nếu để trống sẽ lấy transform của vũ khí")]
    [SerializeField] private Transform attackPoint;

    [Tooltip("Bán kính quét hitbox đòn đánh")]
    [SerializeField] private float attackRadius = 1.6f;

    [Tooltip("Các layer mục tiêu nhận sát thương (Enemy, Default, Resource...)")]
    [SerializeField] private LayerMask hitLayers;

    public Transform AttackPoint => attackPoint;
    public float AttackRadius => attackRadius;
    public LayerMask HitLayers => hitLayers;

    protected override void Awake()
    {
        base.Awake();

        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Enemy", "Default");
            int resourceLayer = LayerMask.NameToLayer("Resource");
            if (resourceLayer != -1)
            {
                hitLayers |= (1 << resourceLayer);
            }
        }
    }

    public override void PerformAttack()
    {
        base.PerformAttack();

        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
        int mask = hitLayers.value != 0 ? hitLayers.value : ~LayerMask.GetMask("Player", "player");

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius, mask);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.transform.root == transform.root) continue;

            HandleCollision(hit);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
