using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý tập trung các bức tường / rào chắn (ZoneWall) giữa các vùng.
/// Theo dõi cấp độ người chơi từ PlayerExperience và tự động mở các bức tường khi đủ điều kiện.
/// </summary>
public class ZoneWallManager : MonoBehaviour
{
    public static ZoneWallManager Instance { get; private set; }

    [Header("Danh Sách Tường Được Quản Lý")]
    [Tooltip("Danh sách các ZoneWall được quản lý. Nếu để trống, hệ thống sẽ tự động quét các ZoneWall con hoặc trong scene.")]
    [SerializeField] private List<ZoneWall> walls = new List<ZoneWall>();

    public IReadOnlyList<ZoneWall> Walls => walls;

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

        RefreshWallList();
    }

    private void Start()
    {
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged += CheckAllWalls;
            CheckAllWalls(PlayerExperience.Instance.CurrentLevel);
        }
    }

    private void OnDestroy()
    {
        if (PlayerExperience.Instance != null)
        {
            PlayerExperience.Instance.OnLevelChanged -= CheckAllWalls;
        }
    }

    /// <summary>
    /// Quét lại toàn bộ các ZoneWall trong GameObject con hoặc trong scene
    /// </summary>
    public void RefreshWallList(bool forceRescan = false)
    {
        if (walls == null) walls = new List<ZoneWall>();

        if (forceRescan || walls.Count == 0)
        {
            ZoneWall[] foundWalls = GetComponentsInChildren<ZoneWall>(true);
            if (foundWalls == null || foundWalls.Length == 0)
            {
                foundWalls = Object.FindObjectsByType<ZoneWall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            walls.Clear();
            if (foundWalls != null && foundWalls.Length > 0)
            {
                walls.AddRange(foundWalls);
            }
        }
        else
        {
            walls.RemoveAll(w => w == null);
        }
    }

    /// <summary>
    /// Đăng ký thêm 1 bức tường vào hệ thống quản lý
    /// </summary>
    public void RegisterWall(ZoneWall wall)
    {
        if (wall != null && !walls.Contains(wall))
        {
            walls.Add(wall);
            if (PlayerExperience.Instance != null)
            {
                wall.CheckUnlockState(PlayerExperience.Instance.CurrentLevel);
            }
        }
    }

    /// <summary>
    /// Gỡ bỏ bức tường khỏi danh sách quản lý
    /// </summary>
    public void UnregisterWall(ZoneWall wall)
    {
        if (wall != null && walls.Contains(wall))
        {
            walls.Remove(wall);
        }
    }

    /// <summary>
    /// Kiểm tra và mở khóa tất cả các bức tường thỏa mãn cấp độ của người chơi
    /// </summary>
    public void CheckAllWalls(int playerLevel)
    {
        walls.RemoveAll(w => w == null);

        foreach (var wall in walls)
        {
            if (wall != null)
            {
                wall.CheckUnlockState(playerLevel);
            }
        }
    }
}
