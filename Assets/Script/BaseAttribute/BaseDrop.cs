using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DropItem
{
    public string name;
    public GameObject itemPrefab;
    [Range(0f, 1f)] public float dropChance;
    public int minAmount;
    public int maxAmount;
}

public class BaseDrop : MonoBehaviour
{
    [Header("Loot Table Settings")]
    [SerializeField] protected List<DropItem> dropList = new List<DropItem>();

    [Header("Scatter Settings")]
    [SerializeField] protected float scatterRadius = 0.5f;

    protected BaseHealth baseHealth;

    protected virtual void Awake()
    {
        baseHealth = GetComponent<BaseHealth>();
    }

    protected virtual void OnEnable()
    {
        if (baseHealth != null)
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

    protected virtual void HandleDeath(GameObject source)
    {
        Drop();
    }

    public virtual void Drop()
    {
        Drop(transform.position);
    }

    public virtual void Drop(Vector3 spawnPosition)
    {
        if (PoolManager.Instance == null || dropList == null) return;

        foreach (DropItem item in dropList)
        {
            if (item.itemPrefab == null) continue;

            if (Random.value <= item.dropChance)
            {
                int count = Random.Range(item.minAmount, item.maxAmount + 1);
                for (int i = 0; i < count; i++)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-scatterRadius, scatterRadius),
                        Random.Range(-scatterRadius, scatterRadius),
                        0f
                    );

                    PoolManager.Instance.Spawn(item.itemPrefab, spawnPosition + offset, Quaternion.identity);
                }
            }
        }
    }
}
