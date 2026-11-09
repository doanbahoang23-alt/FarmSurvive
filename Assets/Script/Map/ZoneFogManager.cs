using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FogZoneItem
{
    public string zoneName = "Vùng";
    public int requiredLevel = 3;
    public GameObject fogObject;
}

public class ZoneFogManager : MonoBehaviour
{
    public static ZoneFogManager Instance { get; private set; }
    [SerializeField] private List<FogZoneItem> fogZones = new List<FogZoneItem>();

    public List<FogZoneItem> FogZones => fogZones;

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

        EnsureCanvasSetup();
    }

    private void Start()
    {
        EnsureCanvasSetup();

        int level = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
        CheckAllZones(level);

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
        CheckAllZones(PlayerExperience.Instance.CurrentLevel);
    }

    public void EnsureCanvasSetup()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponentInChildren<Canvas>();

        if (canvas != null)
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
        CheckAllZones(newLevel);
    }

    public void CheckAllZones(int playerLevel)
    {
        foreach (var item in fogZones)
        {
            if (item != null && item.fogObject != null)
            {
                bool isLocked = playerLevel < item.requiredLevel;
                item.fogObject.SetActive(isLocked);
            }
        }
    }

}

