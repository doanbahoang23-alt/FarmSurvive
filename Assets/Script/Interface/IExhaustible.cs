using UnityEngine;

public interface IExhaustible
{
    void UseStamina(float amount);
    bool HasEnoughStamina(float amount);
}