using UnityEngine;
using System;

public class BaseHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;

    [Header("Regen Settings (Tùy chọn tự hồi máu)")]
    [SerializeField] private bool enableOutOfCombatRegen = false;
    [SerializeField] private float healthRegen = 1f;
    [SerializeField] private float timeOutCombat = 3f;

    [Header("Death Behavior (Hành vi khi hết máu)")]
    [SerializeField] protected bool releaseToPoolOnDeath = false;
    [SerializeField] protected bool destroyOnDeath = false;

    protected float currentHealth;
    private float lastTimeCombat;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; protected set; }

    public event Action<GameObject> OnDeath;
    public event Action<float, float> OnHealthChanged;

    protected virtual void OnEnable()
    {
        currentHealth = maxHealth;
        IsDead = false;
        NotifyHealthChanged();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        lastTimeCombat = -timeOutCombat;
        NotifyHealthChanged();
    }

    protected virtual void Update()
    {
        HandleHealthRegen();
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (IsDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        RestartCombatTime();
        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetHealth(float current, float max = -1f)
    {
        if (max > 0) maxHealth = max;
        currentHealth = Mathf.Clamp(current, 0, maxHealth);
        IsDead = currentHealth <= 0;
        NotifyHealthChanged();
    }

    public void RestartCombatTime()
    {
        lastTimeCombat = Time.time;
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath?.Invoke(gameObject);

        if (releaseToPoolOnDeath && PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public virtual void ResetHealth()
    {
        IsDead = false;
        currentHealth = maxHealth;
        RestartCombatTime();
        NotifyHealthChanged();
    }

    public virtual void RegentHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        NotifyHealthChanged();
    }

    private void HandleHealthRegen()
    {
        if (!enableOutOfCombatRegen || IsDead || currentHealth >= maxHealth || healthRegen <= 0f) return;

        if (Time.time >= lastTimeCombat + timeOutCombat)
        {
            RegentHealth(healthRegen * Time.deltaTime);
        }
    }

    protected void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
