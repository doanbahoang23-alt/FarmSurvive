using System.Linq.Expressions;
using UnityEngine;

public class BaseSpeed : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] protected float baseMoveSpeed = 5f;
    [SerializeField] protected float sprint = 1.5f; //tốc độ thêm 1.5 lần
    protected float buffBonusSpeed = 0f;
    protected float currentSlowMultiplier = 1f;

    public virtual float MoveSpeed(bool isSprinting)
    {
        float currentSpeed = (baseMoveSpeed + buffBonusSpeed) * currentSlowMultiplier;
        if (isSprinting)
        {
            currentSpeed *= sprint;
        }

        return currentSpeed;
    }

    public void SetSlowMultiplier(float multiplier)
    {
        currentSlowMultiplier = multiplier;
    }
}