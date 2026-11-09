using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Data Sources")]
    [SerializeField] private BaseHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private PlayerFood playerFood;

    [Header("Health UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Stamina UI")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Food UI")]
    [SerializeField] private Image foodBarFill;
    [SerializeField] private TextMeshProUGUI foodText;

    [Header("Exp UI")]
    [SerializeField] private Image expBarFill;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI levelText;

    private void Awake()
    {
        FindDataSourcesIfNull();
    }

    private void Start()
    {
        InitializeHUD();
    }

    private void FindDataSourcesIfNull()
    {
        if (playerHealth == null)
        {
            playerHealth = Object.FindFirstObjectByType<BaseHealth>();
        }

        if (playerStamina == null)
        {
            playerStamina = Object.FindFirstObjectByType<PlayerStamina>();
        }

        if (playerExperience == null)
        {
            playerExperience = PlayerExperience.Instance;
            if (playerExperience == null)
            {
                playerExperience = Object.FindFirstObjectByType<PlayerExperience>();
            }
        }

        if (playerFood == null)
        {
            playerFood = PlayerFood.Instance;
            if (playerFood == null)
            {
                playerFood = Object.FindFirstObjectByType<PlayerFood>();
            }
        }
    }

    private void InitializeHUD()
    {
        if (playerHealth != null)
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);

        if (playerFood != null)
        {
            UpdateFoodUI(playerFood.CurrentFood, playerFood.MaxFood);
        }
        else
        {
            if (foodBarFill != null) foodBarFill.fillAmount = 1f;
            if (foodText != null) foodText.text = "100 / 100";
        }

        if (playerExperience != null)
        {
            UpdateExpUI(playerExperience.CurrentExp, playerExperience.MaxExp);
            UpdateLevelUI(playerExperience.CurrentLevel);
        }
        else
        {
            if (expBarFill != null) expBarFill.fillAmount = 0f;
            if (expText != null) expText.text = "0 / 100";
            if (levelText != null) levelText.text = "Lv. 1";
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateHealthUI;

        if (playerStamina != null)
            playerStamina.OnStaminaChanged += UpdateStaminaUI;

        if (playerFood != null)
        {
            playerFood.OnFoodChanged += UpdateFoodUI;
            UpdateFoodUI(playerFood.CurrentFood, playerFood.MaxFood);
        }

        if (playerExperience != null)
        {
            playerExperience.OnExpChanged += UpdateExpUI;
            playerExperience.OnLevelChanged += UpdateLevelUI;
            UpdateExpUI(playerExperience.CurrentExp, playerExperience.MaxExp);
            UpdateLevelUI(playerExperience.CurrentLevel);
        }
        else
        {
            if (expBarFill != null) expBarFill.fillAmount = 0f;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthUI;

        if (playerStamina != null)
            playerStamina.OnStaminaChanged -= UpdateStaminaUI;

        if (playerFood != null)
            playerFood.OnFoodChanged -= UpdateFoodUI;

        if (playerExperience != null)
        {
            playerExperience.OnExpChanged -= UpdateExpUI;
            playerExperience.OnLevelChanged -= UpdateLevelUI;
        }
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = max > 0 ? (current / max) : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void UpdateStaminaUI(float current, float max)
    {
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = max > 0 ? (current / max) : 0f;

        if (staminaText != null)
            staminaText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void UpdateFoodUI(float current, float max)
    {
        if (foodBarFill != null)
            foodBarFill.fillAmount = max > 0 ? (current / max) : 0f;

        if (foodText != null)
            foodText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void UpdateExpUI(float current, float max)
    {
        if (expBarFill != null)
        {
            expBarFill.fillAmount = max > 0 ? (current / max) : 0f;
        }

        if (expText != null)
            expText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void UpdateLevelUI(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv. {level}";
    }
}
