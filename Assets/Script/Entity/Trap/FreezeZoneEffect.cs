using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeZoneEffect : MonoBehaviour, IPlaceable
{
    [SerializeField] private float timeToFreeze = 5f;
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private float frostDamagePerSecond = 5f;
    [SerializeField] private float frostMaintainDuration = 5f;

    private Dictionary<Collider2D, float> targetTimers = new Dictionary<Collider2D, float>();
    private Dictionary<Collider2D, float> targetCooldowns = new Dictionary<Collider2D, float>();

    private void Update()
    {
        List<Collider2D> activeTargets = new List<Collider2D>(targetTimers.Keys);

        foreach (Collider2D target in activeTargets)
        {
            if (target == null)
            {
                targetTimers.Remove(target);
                targetCooldowns.Remove(target);
                continue;
            }

            if (targetCooldowns.ContainsKey(target))
            {
                targetCooldowns[target] -= Time.deltaTime;
                if (targetCooldowns[target] <= 0f)
                {
                    targetCooldowns.Remove(target);
                }
                continue;
            }

            targetTimers[target] += Time.deltaTime;

            if (targetTimers[target] >= timeToFreeze)
            {
                ApplyFreezeEffect(target);

                targetTimers[target] = 0f;
                targetCooldowns[target] = freezeDuration;
            }
        }
    }

    private void ApplyFreezeEffect(Collider2D target)
    {
        if (target == null) return;

        IFreezable freezable = target.GetComponent<IFreezable>() ?? target.GetComponentInParent<IFreezable>();
        if (freezable != null)
        {
            freezable.ApplyFreeze(freezeDuration, frostMaintainDuration, frostDamagePerSecond);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!targetTimers.ContainsKey(collision))
        {
            targetTimers.Add(collision, 0f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (targetTimers.ContainsKey(collision))
        {
            targetTimers.Remove(collision);
        }
        if (targetCooldowns.ContainsKey(collision))
        {
            targetCooldowns.Remove(collision);
        }
    }
}
