using System;
using UnityEngine;

public class PlayerExperience : MonoBehaviour, IExpReceivable
{
    public static PlayerExperience Instance { get; private set; }

    [Header("Cấu hình Cấp Độ & EXP (Tùy chỉnh trong Inspector)")]
    [Tooltip("Lượng EXP yêu cầu tối thiểu ở Cấp 1 (thanh kinh nghiệm ban đầu)")]
    [SerializeField] private float baseExpLevel1 = 100f;

    [Tooltip("Hệ số tăng trưởng EXP qua từng cấp (ví dụ: 1.25 nghĩa là mỗi cấp tăng thêm 25% EXP)")]
    [SerializeField] private float expGrowthMultiplier = 1.25f;

    [Header("Trạng thái Thực tế (Runtime Status)")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float maxExp = 100f;

    public float BaseExpLevel1 => baseExpLevel1;
    public float ExpGrowthMultiplier => expGrowthMultiplier;

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float MaxExp => maxExp;

    public event Action<float, float> OnExpChanged;
    public event Action<int> OnLevelChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void OnValidate()
    {
        if (baseExpLevel1 < 1f) baseExpLevel1 = 1f;
        if (expGrowthMultiplier < 1f) expGrowthMultiplier = 1f;

        // Trong Editor khi chưa Play, nếu đang ở cấp 1 thì cập nhật maxExp theo baseExpLevel1
        if (!Application.isPlaying && currentLevel <= 1)
        {
            maxExp = baseExpLevel1;
        }
    }

    private void Start()
    {
        // Đảm bảo các chỉ số tối thiểu hợp lệ theo biến cấu hình Inspector
        if (baseExpLevel1 < 1f) baseExpLevel1 = 100f;
        if (expGrowthMultiplier < 1f) expGrowthMultiplier = 1.25f;

        // Nếu mới vào game ở Cấp 1 và maxExp chưa khớp với baseExpLevel1 cấu hình trong Inspector
        if (currentLevel <= 1 && (maxExp < baseExpLevel1 || maxExp <= 0f))
        {
            maxExp = baseExpLevel1;
        }

        NotifyExpChanged();
        NotifyLevelChanged();
    }

    public void AddExp(float amount)
    {
        if (amount <= 0) return;

        float minExp = baseExpLevel1 > 0f ? baseExpLevel1 : 100f;
        float mult = expGrowthMultiplier > 1f ? expGrowthMultiplier : 1.25f;

        if (maxExp < minExp) maxExp = minExp;

        currentExp += amount;

        int safetyIterationLimit = 500; // Bảo vệ chống treo Unity nếu cộng lượng EXP quá lớn
        while (currentExp >= maxExp && safetyIterationLimit-- > 0)
        {
            currentExp -= maxExp;
            currentLevel++;
            maxExp = Mathf.Round(maxExp * mult);
            NotifyLevelChanged();
        }

        NotifyExpChanged();
    }

    /// <summary>
    /// Đặt lại EXP và Cấp độ về ban đầu dựa trên biến cấu hình Inspector baseExpLevel1 (không set cứng).
    /// </summary>
    public void ResetExperience()
    {
        currentLevel = 1;
        currentExp = 0f;
        maxExp = baseExpLevel1 > 0f ? baseExpLevel1 : 100f;
        NotifyExpChanged();
        NotifyLevelChanged();
    }

    public void NotifyExpChanged()
    {
        OnExpChanged?.Invoke(currentExp, maxExp);
    }

    public void NotifyLevelChanged()
    {
        OnLevelChanged?.Invoke(currentLevel);
    }

    public void SetExpAndLevel(int level, float exp, float max)
    {
        currentLevel = Mathf.Max(1, level);
        float minExp = baseExpLevel1 > 0f ? baseExpLevel1 : 100f;
        maxExp = max > 0f ? max : minExp;
        currentExp = Mathf.Clamp(exp, 0f, maxExp);
        NotifyExpChanged();
        NotifyLevelChanged();
    }
}
