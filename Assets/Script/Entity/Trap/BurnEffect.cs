using UnityEngine;

public class BurnEffect : MonoBehaviour, IPlaceable
{
    [SerializeField] private float burnDamagePerSecond = 5f;
    [SerializeField] private float burnDuration = 3f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        IBurnable burnable = collision.GetComponent<IBurnable>() ?? collision.GetComponentInParent<IBurnable>();
        if (burnable != null)
        {
            burnable.ApplyBurn(burnDamagePerSecond, burnDuration);
        }
    }
}
