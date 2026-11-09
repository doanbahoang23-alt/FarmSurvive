using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour, IExhaustible
{
    [Header("Cấu hình Thể Lực (Stamina)")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float lostStamina = 30f;
    [SerializeField] private float regenStamina = 15f;

    private float currentStamina;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float Lost => lostStamina;
    public float Regen => regenStamina;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Start()
    {
        NotifyStaminaChanged();
    }

    public void UseStamina(float amount)
    {
        if (amount <= 0) return;

        currentStamina -= amount;
        if (currentStamina < 0f)
        {
            currentStamina = 0f;
        }
        NotifyStaminaChanged();
    }

    public bool HasEnoughStamina(float amount)
    {
        return currentStamina >= amount;
    }

    public void RegenStamina(float amount)
    {
        if (amount <= 0) return;

        if (currentStamina < maxStamina)
        {
            currentStamina += amount;
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
            NotifyStaminaChanged();
        }
    }

    public void SetStamina(float current, float max)
    {
        maxStamina = Mathf.Max(1f, max);
        currentStamina = Mathf.Clamp(current, 0f, maxStamina);
        NotifyStaminaChanged();
    }

    private void NotifyStaminaChanged()
    {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
