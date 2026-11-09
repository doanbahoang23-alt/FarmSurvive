using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ZoneWall : MonoBehaviour
{
    [Header("Cấu hình Tường Ngăn Cách")]
    [Tooltip("Tên định danh hiển thị (để trống sẽ tự động lấy theo tên GameObject trên Hierarchy)")]
    [SerializeField] private string customWallName = "";

    [Tooltip("Loại vùng mà bức tường này đang bảo vệ / ngăn chặn")]
    [SerializeField] private ZoneType targetZoneType = ZoneType.Forest;

    [Tooltip("Cấp độ yêu cầu của người chơi để mở bức tường này")]
    [SerializeField] private int requiredLevel = 3;

    [Header("Màu sắc hiển thị khi chọn trong Scene")]
    [SerializeField] private Color wallColor = new Color(1f, 0.2f, 0.2f, 0.85f);

    public ZoneType TargetZone => targetZoneType;
    public int RequiredLevel => requiredLevel;
    public Color WallColor => wallColor;

    public string WallName => !string.IsNullOrEmpty(customWallName) ? customWallName : gameObject.name;

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {

            boxCollider.isTrigger = false;
        }
    }

    private void Start()
    {
        if (ZoneWallManager.Instance != null)
        {
            ZoneWallManager.Instance.RegisterWall(this);
        }

        int currentLevel = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
        CheckUnlockState(currentLevel);

        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged += OnPlayerLevelChanged;
        }
    }

    private void OnDestroy()
    {
        if (ZoneWallManager.Instance != null)
        {
            ZoneWallManager.Instance.UnregisterWall(this);
        }

        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged -= OnPlayerLevelChanged;
        }
    }

    public void Setup(ZoneType zoneType, int level, string name = null, Color? color = null)
    {
        targetZoneType = zoneType;
        requiredLevel = level;
        if (!string.IsNullOrEmpty(name))
        {
            customWallName = name;
        }
        if (color.HasValue)
        {
            wallColor = color.Value;
        }
    }

    private void OnPlayerLevelChanged(int newLevel)
    {
        CheckUnlockState(newLevel);
    }

    public void CheckUnlockState(int currentLevel)
    {
        if (currentLevel >= requiredLevel)
        {
            OpenWall();
        }
        else
        {
            // Chưa đủ cấp độ: Đảm bảo tường luôn đóng (chặn người chơi)
            CloseWall();
        }
    }

    /// <summary>
    /// Mở tường rào chắn khi người chơi đạt đủ cấp độ
    /// </summary>
    public void OpenWall()
    {
        int currentLevel = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
        gameObject.SetActive(false);
        Debug.Log($"<color=cyan>[RÀO CHẮN]</color> {WallName} đã mở ra! (Cấp yêu cầu: {requiredLevel}, Cấp hiện tại của Player: {currentLevel})");
    }


    public void CloseWall()
    {
        gameObject.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            int currentLevel = PlayerExperience.Instance != null ? PlayerExperience.Instance.CurrentLevel : 1;
            Debug.Log($"<color=yellow>[CẢNH BÁO]</color> {WallName}: Yêu cầu Cấp {requiredLevel} để đi qua! (Cấp hiện tại: {currentLevel})");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        // Chỉ vẽ khung viền khi GameObject được chọn trong Hierarchy để không làm rối Scene
        Gizmos.color = wallColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.offset, col.size);

        Color fill = wallColor;
        fill.a = 0.35f;
        Gizmos.color = fill;
        Gizmos.DrawCube(col.offset, col.size);

        Vector3 worldCenter = transform.TransformPoint(col.offset);
        GUIStyle style = new GUIStyle();
        style.normal.textColor = wallColor;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        UnityEditor.Handles.Label(worldCenter, $"[CẤP {requiredLevel}]\n{WallName}", style);
    }

    [ContextMenu("Mở Tường (Test)")]
    private void ContextMenuOpenWall()
    {
        OpenWall();
    }

    [ContextMenu("Đóng Tường (Test)")]
    private void ContextMenuCloseWall()
    {
        CloseWall();
    }
#endif
}
