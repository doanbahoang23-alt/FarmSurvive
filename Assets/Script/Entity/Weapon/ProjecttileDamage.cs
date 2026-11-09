using UnityEngine;
using System.Collections;

public class ProjectileDamage : BaseDamage
{
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private string obstacleTag = "Environment";
    private Coroutine lifeTimer;

    private void OnEnable()
    {
        lifeTimer = StartCoroutine(ReleaseAfterTime());
    }

    private void OnDisable()
    {
        if (lifeTimer != null)
        {
            StopCoroutine(lifeTimer);
            lifeTimer = null;
        }
    }

    private IEnumerator ReleaseAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.root == transform.root) return;

        if (collision.CompareTag(targetTag))
        {
            if (collision.TryGetComponent(out IDamageable damageable) || (damageable = collision.GetComponentInParent<IDamageable>()) != null)
            {
                damageable.TakeDamage(AttackDamage());
            }

            if (collision.TryGetComponent(out IKnockbackable knockbackable))
            {
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                knockbackable.ApplyKnockback(knockbackDir, currentKnockbackForce);
            }

            ReturnToPool();
        }
        else if (collision.CompareTag(obstacleTag) || collision.CompareTag("Wall"))
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    public void SetupProjectile(float damage, float knockback)
    {
        currentBaseDamage = damage;
        currentKnockbackForce = knockback;
    }
}
