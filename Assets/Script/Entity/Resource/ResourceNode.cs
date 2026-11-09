using System.Collections;
using UnityEngine;

public enum ResourceType
{
    Tree,
    Rock
}

[RequireComponent(typeof(Collider2D))]
public class ResourceNode : MonoBehaviour, IDamageable
{
    [Header("Cấu hình Loại Tài Nguyên")]
    [SerializeField] private ResourceType resourceType = ResourceType.Tree;

    [Header("Cơ chế Hồi sinh Tài nguyên")]
    [SerializeField] private bool canRespawn = true;// cho phép repspawn

    [SerializeField] private float respawnTime = 60f;// time spawn
    [SerializeField] private float safetyCheckPadding = 0.2f;// check player save

    private BaseHealth baseHealth;
    private SpriteRenderer spriteRenderer;
    private Collider2D nodeCollider;
    private Coroutine respawnCoroutine;

    public ResourceType Type => resourceType;

    public bool CanHarvestWith(ToolType tool)
    {
        return ToolManager.CanHarvestResource(tool, resourceType);
    }

    public void TakeDamage(float damageAmount)
    {
        if (baseHealth == null)
        {
            baseHealth = GetComponent<BaseHealth>() ?? GetComponentInParent<BaseHealth>() ?? GetComponentInChildren<BaseHealth>();
        }

        if (baseHealth != null)
        {
            baseHealth.TakeDamage(damageAmount);
        }
    }

    private void Awake()
    {
        baseHealth = GetComponent<BaseHealth>();
        nodeCollider = GetComponent<Collider2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (baseHealth != null)
        {
            baseHealth.OnDeath -= OnResourceDestroyed;
            baseHealth.OnDeath += OnResourceDestroyed;
        }

        SetVisualAndPhysicsActive(true);
    }

    private void OnDisable()
    {
        if (baseHealth != null)
        {
            baseHealth.OnDeath -= OnResourceDestroyed;
        }

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
    }

    private void OnResourceDestroyed(GameObject source)
    {
        SetVisualAndPhysicsActive(false);

        if (canRespawn)
        {
            if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
            respawnCoroutine = StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        while (IsPlayerOverlapping())
        {
            yield return new WaitForSeconds(1.0f);
        }

        Respawn();
        respawnCoroutine = null;
    }

    private bool IsPlayerOverlapping()
    {
        if (nodeCollider == null) return false;

        Vector2 checkPos = (Vector2)transform.position + nodeCollider.offset;
        float checkRadius = 0.6f;

        if (nodeCollider is CircleCollider2D circle)
        {
            checkRadius = (circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y)) + safetyCheckPadding;
        }
        else if (nodeCollider is BoxCollider2D box)
        {
            float maxSide = Mathf.Max(box.size.x * transform.lossyScale.x, box.size.y * transform.lossyScale.y);
            checkRadius = (maxSide * 0.5f) + safetyCheckPadding;
        }
        else if (nodeCollider is CapsuleCollider2D capsule)
        {
            float maxSide = Mathf.Max(capsule.size.x * transform.lossyScale.x, capsule.size.y * transform.lossyScale.y);
            checkRadius = (maxSide * 0.5f) + safetyCheckPadding;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit == nodeCollider) continue;

            if (hit.CompareTag("Player") || hit.GetComponentInParent<PlayerController>() != null)
            {
                return true; // Có Player đang đứng đè lên
            }
        }

        return false;
    }

    public void Respawn()
    {
        SetVisualAndPhysicsActive(true);

        if (baseHealth != null)
        {
            baseHealth.ResetHealth();
        }
    }

    private void SetVisualAndPhysicsActive(bool isActive)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = isActive;
        }

        if (nodeCollider != null)
        {
            nodeCollider.enabled = isActive;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 checkPos = (Vector2)transform.position + col.offset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(checkPos, 0.6f + safetyCheckPadding);
        }
    }
}
