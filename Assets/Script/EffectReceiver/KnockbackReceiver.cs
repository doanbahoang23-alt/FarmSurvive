using UnityEngine;

public class KnockbackReceiver : MonoBehaviour, IKnockbackable
{
    [SerializeField] private float knockbackTime = 0.2f;
    private float knockbackCounter;

    public bool IsKnockback => knockbackCounter > 0;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.fixedDeltaTime;
            return;
        }
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        knockbackCounter = knockbackTime;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

}