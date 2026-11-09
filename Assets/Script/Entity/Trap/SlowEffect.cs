using UnityEngine;

public class SlowEffect : MonoBehaviour, IPlaceable
{
    [SerializeField] private float percentageSlow = 0.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ISlowable slowable = collision.GetComponent<ISlowable>() ?? collision.GetComponentInParent<ISlowable>();
        if (slowable != null)
        {
            slowable.SetZoneSlow(percentageSlow, true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        ISlowable slowable = collision.GetComponent<ISlowable>() ?? collision.GetComponentInParent<ISlowable>();
        if (slowable != null)
        {
            slowable.SetZoneSlow(percentageSlow, false);
        }
    }
}
