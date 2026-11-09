using UnityEngine;

public class ZoneFog : MonoBehaviour
{
    [Header("Cấu Hình Cấp Độ")]
    [Tooltip("Cấp độ người chơi yêu cầu để tắt lớp che mờ này")]
    [SerializeField] private int requiredLevel = 3;

    public int RequiredLevel => requiredLevel;

    private void Start()
    {
        EnsureCorrectSorting();

        int level = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
        CheckLevel(level);

        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged += OnPlayerLevelChanged;
        }
        else
        {
            StartCoroutine(WaitForPlayerExperience());
        }
    }

    private System.Collections.IEnumerator WaitForPlayerExperience()
    {
        while (PlayerExperience.Instance == null)
        {
            yield return null;
        }

        PlayerExperience.Instance.OnLevelChanged -= OnPlayerLevelChanged;
        PlayerExperience.Instance.OnLevelChanged += OnPlayerLevelChanged;
        CheckLevel(PlayerExperience.Instance.CurrentLevel);
    }

    private void EnsureCorrectSorting()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "fog";
            if (sr.sortingOrder < 10) sr.sortingOrder = 10;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.sortingLayerName = "fog";
            canvas.sortingOrder = 50;
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }

    private void OnDestroy()
    {
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged -= OnPlayerLevelChanged;
        }
    }

    private void OnPlayerLevelChanged(int newLevel)
    {
        CheckLevel(newLevel);
    }

    public void CheckLevel(int currentLevel)
    {
        if (currentLevel >= requiredLevel)
        {
            // Đủ cấp: TẮT che mờ
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            // Chưa đủ cấp: BẬT che mờ
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
    }

    public void RemoveFog()
    {
        gameObject.SetActive(false);
    }
}
