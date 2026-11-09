using UnityEngine;

public class ExpReward : MonoBehaviour
{
    [Header("Cấu hình EXP Thưởng")]
    [SerializeField] protected float expReward = 15f;

    [SerializeField] protected bool giveExpOnDeath = true;

    protected BaseHealth baseHealth;

    public float ExpRewardAmount => expReward;

    protected virtual void Awake()
    {
        baseHealth = GetComponent<BaseHealth>();
    }

    protected virtual void OnEnable()
    {
        if (giveExpOnDeath && baseHealth != null)
        {
            baseHealth.OnDeath -= HandleDeath;
            baseHealth.OnDeath += HandleDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (baseHealth != null)
        {
            baseHealth.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath(GameObject killer)
    {
        GiveExp(killer);
    }

    public virtual void GiveExp(GameObject target = null)
    {
        if (expReward <= 0) return;

        if (target != null && target.TryGetComponent(out IExpReceivable receiver))
        {
            receiver.AddExp(expReward);
            return;
        }

        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.AddExp(expReward);
            return;
        }

        PlayerExperience playerExp = Object.FindFirstObjectByType<PlayerExperience>();
        if (playerExp != null)
        {
            playerExp.AddExp(expReward);
        }
    }
}
