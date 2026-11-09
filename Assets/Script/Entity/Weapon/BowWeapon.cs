using UnityEngine;

public class BowWeapon : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float arrowSpeed = 20f;

    [Header("Âm Thanh")]
    [Tooltip("Âm thanh bắn mũi tên (nếu để trống sẽ tự lấy arrowShootClip từ AudioManager)")]
    [SerializeField] private AudioClip shootSoundClip;

    private BaseDamage bowBaseStarts;

    private float lastShootTime = 0f;

    void Awake()
    {
        bowBaseStarts = GetComponent<BaseDamage>();
    }

    public void Shoot()
    {
        if (Time.time - lastShootTime < 0.25f) return;
        lastShootTime = Time.time;

        if (arrowPrefab == null || firePoint == null)
        {
            return;
        }

        GameObject arrow = PoolManager.Instance != null
            ? PoolManager.Instance.Spawn(arrowPrefab, firePoint.position, firePoint.rotation)
            : Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);

        Collider2D arrowCol = arrow.GetComponent<Collider2D>();
        if (arrowCol != null)
        {
            arrowCol.isTrigger = true;
            Collider2D[] playerCols = transform.root.GetComponentsInChildren<Collider2D>();
            foreach (var pCol in playerCols)
            {
                if (pCol != null && pCol != arrowCol)
                {
                    Physics2D.IgnoreCollision(arrowCol, pCol, true);
                }
            }
        }

        PlayShootAudio();
        if (arrow.TryGetComponent(out ProjectileDamage projectileDamage))
        {
            float dmg = bowBaseStarts != null ? bowBaseStarts.AttackDamage() : 10f;
            projectileDamage.SetupProjectile(dmg, 10f);
        }
        if (arrow.TryGetComponent(out Rigidbody2D rb))
        {
            Animator playerAnim = transform.root.GetComponent<Animator>();
            Vector2 shootDirection;

            if (playerAnim != null)
            {
                float x = playerAnim.GetFloat("LastInputX");
                float y = playerAnim.GetFloat("LastInputY");
                shootDirection = new Vector2(x, y).normalized;

                if (shootDirection == Vector2.zero)
                {
                    shootDirection = new Vector2(Mathf.Sign(transform.root.localScale.x), 0);
                }

                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

                rb.linearVelocity = shootDirection * arrowSpeed;
            }
        }
    }

    private void PlayShootAudio()
    {
        if (shootSoundClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(shootSoundClip);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayArrowShoot();
        }
    }
}