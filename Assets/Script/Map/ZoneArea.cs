using System.Collections.Generic;
using UnityEngine;

public enum ZoneType
{
    Farm = 1,        // Vùng 1: Nơi bắt đầu farm
    Forest = 2,      // Vùng 2: Rừng rậm
    Stone = 3,       // Vùng 3: Nơi farm đá
    MonsterCave = 4  // Vùng 4: Ổ quái vật
}

[RequireComponent(typeof(BoxCollider2D))]
public class ZoneArea : MonoBehaviour
{
    [Header("Cấu hình Khu Vực")]
    [SerializeField] private ZoneType zoneType = ZoneType.Farm;
    [SerializeField] private string zoneName = "Khu Vực";
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private bool isUnlocked = false;

    [Header("Tường / Rào Chắn Liên Kết")]
    [SerializeField] private List<ZoneWall> connectedWalls = new List<ZoneWall>();

    [Header("Hiệu Ứng Sương Mù (ZoneFog)")]
    [Tooltip("Lớp sương mù che phủ khu vực này khi chưa mở khóa")]
    [SerializeField] private ZoneFog zoneFog;

    [Header("Màu sắc Gizmo hiển thị")]
    [SerializeField] private Color zoneColor = Color.green;

    public ZoneType Type => zoneType;
    public string ZoneName => !string.IsNullOrEmpty(zoneName) ? zoneName : gameObject.name;
    public int RequiredLevel => requiredLevel;
    public bool IsUnlocked => isUnlocked;
    public ZoneFog Fog => zoneFog;
    public BoxCollider2D BoxCollider => boxCollider != null ? boxCollider : (boxCollider = GetComponent<BoxCollider2D>());

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        if (zoneFog == null)
        {
            zoneFog = GetComponentInChildren<ZoneFog>();
        }
    }

    private void Start()
    {
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged += OnPlayerLevelChanged;
            CheckUnlockState(PlayerExperience.Instance.CurrentLevel);
        }
        else if (requiredLevel <= 1)
        {
            UnlockZone();
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
        CheckUnlockState(PlayerExperience.Instance.CurrentLevel);
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
        CheckUnlockState(newLevel);
    }

    public void CheckUnlockState(int currentLevel)
    {
        if (!isUnlocked && currentLevel >= requiredLevel)
        {
            UnlockZone();
        }
    }

    public void Setup(ZoneType type, string name, int level, Color color)
    {
        zoneType = type;
        zoneName = name;
        requiredLevel = level;
        zoneColor = color;
    }

    public void AddConnectedWall(ZoneWall wall)
    {
        if (wall != null && !connectedWalls.Contains(wall))
        {
            connectedWalls.Add(wall);
        }
    }

    public void UnlockZone()
    {
        isUnlocked = true;

        if (connectedWalls != null)
        {
            int playerLevel = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
            foreach (var wall in connectedWalls)
            {
                if (wall != null)
                {
                    wall.CheckUnlockState(playerLevel);
                }
            }
        }

        if (zoneFog != null)
        {
            zoneFog.RemoveFog();
        }

    }

}
