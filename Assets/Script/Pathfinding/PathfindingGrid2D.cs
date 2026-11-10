using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfindingGrid2D : MonoBehaviour
{
    #region Static Lookup
    public static PathfindingGrid2D Instance { get; private set; }

    //tọa độ offset hướng chính 
    private static readonly int[] OrthoDx = { 0, 0, -1, 1 };
    private static readonly int[] OrthoDy = { 1, -1, 0, 0 };

    //tọa độ offset 4 góc chéo
    private static readonly int[] DiagDx = { 1, 1, -1, -1 };
    private static readonly int[] DiagDy = { 1, -1, 1, -1 };
    #endregion

    #region Inspector Fields
    [Header("1. Vùng Bao Phủ & Kích Thước (Grid Area)")]
    [Tooltip("Khoảng cách viền đệm cộng thêm khi tự động căn chỉnh từ Editor (đơn vị: mét)")]
    [SerializeField] private float autoFitPadding = 6f;

    [Tooltip("Kích thước lưới thế giới (Chiều rộng X, Chiều cao Y)")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(160f, 160f);

    [Tooltip("Tọa độ tâm của lưới trên bản đồ thế giới")]
    [SerializeField] private Vector2 gridCenter = Vector2.zero;

    [Tooltip("Bán kính của mỗi ô Node (0.5 = ô rộng 1x1 mét chuẩn Tilemap)")]
    [SerializeField] private float nodeRadius = 0.5f;

    [Header("2. Layer Vật Cản (Obstacles)")]
    [Tooltip("Các Layer được xem là vật cản không thể đi qua (Sông, Tường, Công trình)")]
    [SerializeField] private LayerMask obstacleLayers;

    [Tooltip("Bán kính tia quét vật lý trong mỗi ô (nên nhỏ hơn nodeRadius để tránh chạm nhầm biên)")]
    [SerializeField] private float obstacleCheckRadius = 0.4f;

    [Header("3. Hiển Thị Kiểm Tra Trong Scene (Gizmos)")]
    [Tooltip("Chỉ bật khi cần debug lưới. Mặc định tắt để tránh giảm FPS trên bản đồ lớn.")]
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private bool onlyShowObstacles = true;
    [Tooltip("Bán kính tối đa quanh Camera/Player để vẽ Gizmos (tránh vẽ hàng trăm nghìn ô gây đơ máy)")]
    [SerializeField] private float gizmoDrawRadius = 25f;
    #endregion

    #region Runtime Data
    private PathNode2D[,] grid;
    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;
    private Vector3 worldBottomLeft;

    public float NodeDiameter => nodeDiameter;
    public int GridSizeX => gridSizeX;
    public int GridSizeY => gridSizeY;
    public Vector2 GridWorldSize => gridWorldSize;
    public Vector2 GridCenter => gridCenter;
    #endregion

    #region Unity Lifecycle (Orchestrator)
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitObstacleLayersIfEmpty();
        BuildGrid();
    }
    #endregion

    #region Grid Construction
    private void InitObstacleLayersIfEmpty()
    {
        if (obstacleLayers.value == 0)
        {
            obstacleLayers = LayerMask.GetMask("wall", "Water", "Building");
            if (obstacleLayers.value == 0) obstacleLayers = LayerMask.GetMask("wall", "Water");
            if (obstacleLayers.value == 0) obstacleLayers = (1 << 4) | (1 << 7); // Layer 4 (Water), Layer 7 (wall)
        }
    }

    [ContextMenu("Xây Dựng Lại Lưới (Rebuild Grid)")]
    public void BuildGrid()
    {
        InitObstacleLayersIfEmpty();

        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.x / nodeDiameter));
        gridSizeY = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.y / nodeDiameter));

        grid = new PathNode2D[gridSizeX, gridSizeY];

        float halfWidth = gridWorldSize.x * 0.5f;
        float halfHeight = gridWorldSize.y * 0.5f;
        float bottomLeftX = gridCenter.x - halfWidth;
        float bottomLeftY = gridCenter.y - halfHeight;
        worldBottomLeft = new Vector3(bottomLeftX, bottomLeftY, 0f);

        // 1. Khởi tạo toàn bộ Node mặc định là Đi Bộ Được (Walkable = true) siêu tốc (0 Physics query)
        for (int x = 0; x < gridSizeX; x++)
        {
            float worldX = bottomLeftX + (x * nodeDiameter) + nodeRadius;

            for (int y = 0; y < gridSizeY; y++)
            {
                float worldY = bottomLeftY + (y * nodeDiameter) + nodeRadius;
                Vector3 worldPos = new Vector3(worldX, worldY, 0f);
                grid[x, y] = new PathNode2D(true, worldPos, x, y);
            }
        }

        // 2. Tối ưu hóa cực hạn: Chỉ quét va chạm vật lý tại các khu vực thực sự có Collider2D thuộc obstacleLayers
        Collider2D[] allColliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<Collider2D> obstacleColliders = new List<Collider2D>();
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D col = allColliders[i];
            if (col != null && !col.isTrigger && ((1 << col.gameObject.layer) & obstacleLayers.value) != 0)
            {
                obstacleColliders.Add(col);
            }
        }

        // 3. Duyệt qua từng Collider vật cản và chỉ quét các Node nằm trong Bounding Box của nó
        for (int i = 0; i < obstacleColliders.Count; i++)
        {
            Collider2D col = obstacleColliders[i];
            Bounds b = col.bounds;

            int startX = Mathf.Clamp(Mathf.FloorToInt((b.min.x - bottomLeftX - obstacleCheckRadius) / nodeDiameter), 0, gridSizeX - 1);
            int endX = Mathf.Clamp(Mathf.CeilToInt((b.max.x - bottomLeftX + obstacleCheckRadius) / nodeDiameter), 0, gridSizeX - 1);
            int startY = Mathf.Clamp(Mathf.FloorToInt((b.min.y - bottomLeftY - obstacleCheckRadius) / nodeDiameter), 0, gridSizeY - 1);
            int endY = Mathf.Clamp(Mathf.CeilToInt((b.max.y - bottomLeftY + obstacleCheckRadius) / nodeDiameter), 0, gridSizeY - 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    PathNode2D node = grid[x, y];
                    if (!node.IsWalkable) continue; // Đã đánh dấu là cản rồi thì không cần kiểm tra lại

                    if (!CheckPositionWalkable(node.WorldPosition))
                    {
                        node.IsWalkable = false;
                    }
                }
            }
        }
    }
    public bool CheckPositionWalkable(Vector2 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, obstacleCheckRadius, obstacleLayers);
        return hit == null || hit.isTrigger;
    }
    #endregion

    #region Editor Tools (Bounds Fitting)
    [ContextMenu("Tự Động Căn Chỉnh Theo Tilemap (Fit Grid To Tilemaps)")]
    public void FitGridToSceneTilemaps()
    {
        if (TryCalculateTilemapBounds(out Vector2 newCenter, out Vector2 newSize))
        {
            gridCenter = newCenter;
            gridWorldSize = newSize;
        }
    }

    public bool TryCalculateTilemapBounds(out Vector2 center, out Vector2 size)
    {
        center = gridCenter;
        size = gridWorldSize;

        TilemapRenderer[] renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        center = combinedBounds.center;
        size = new Vector2(
            combinedBounds.size.x + (autoFitPadding * 2f),
            combinedBounds.size.y + (autoFitPadding * 2f)
        );
        return true;
    }
    #endregion

    #region Coordinate & Query Queries

    public PathNode2D GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        if (grid == null) BuildGrid();

        float percentX = Mathf.Clamp01((worldPosition.x - worldBottomLeft.x) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.y - worldBottomLeft.y) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.FloorToInt(percentX * gridSizeX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(percentY * gridSizeY), 0, gridSizeY - 1);

        return grid[x, y];
    }

    public bool IsWorldPositionWalkable(Vector3 worldPosition)
    {
        PathNode2D node = GetNodeFromWorldPosition(worldPosition);
        return node != null && node.IsWalkable;
    }

    public List<PathNode2D> GetNeighborNodes(PathNode2D node, bool allowDiagonals = true)
    {
        if (node == null || grid == null) return new List<PathNode2D>(0);

        List<PathNode2D> neighbors = new List<PathNode2D>(8);
        int nodeX = node.GridX;
        int nodeY = node.GridY;

        AddOrthogonalNeighbors(nodeX, nodeY, neighbors);

        if (allowDiagonals)
        {
            AddDiagonalNeighbors(nodeX, nodeY, neighbors);
        }

        return neighbors;
    }

    private void AddOrthogonalNeighbors(int nodeX, int nodeY, List<PathNode2D> neighbors)
    {
        for (int i = 0; i < 4; i++)
        {
            int checkX = nodeX + OrthoDx[i];
            int checkY = nodeY + OrthoDy[i];

            if (IsValidGridCoordinate(checkX, checkY))
            {
                neighbors.Add(grid[checkX, checkY]);
            }
        }
    }

    private void AddDiagonalNeighbors(int nodeX, int nodeY, List<PathNode2D> neighbors)
    {
        for (int i = 0; i < 4; i++)
        {
            int checkX = nodeX + DiagDx[i];
            int checkY = nodeY + DiagDy[i];

            if (!IsValidGridCoordinate(checkX, checkY)) continue;

            // Chống lỗi đi cắt xuyên góc tường (Corner Cutting Protection):
            // Chỉ cho đi chéo khi cả 2 ô liền kề trực giao đều không bị tường chắn
            int adjX = checkX;
            int adjY = nodeY;
            int perpX = nodeX;
            int perpY = checkY;

            bool adjWalkable = IsValidGridCoordinate(adjX, adjY) && grid[adjX, adjY].IsWalkable;
            bool perpWalkable = IsValidGridCoordinate(perpX, perpY) && grid[perpX, perpY].IsWalkable;

            if (adjWalkable && perpWalkable)
            {
                neighbors.Add(grid[checkX, checkY]);
            }
        }
    }

    public PathNode2D GetClosestWalkableNode(PathNode2D node, int searchRadius = 4)
    {
        if (node == null || node.IsWalkable) return node;

        PathNode2D bestNode = null;
        float closestDist = float.MaxValue;
        Vector3 nodePos = node.WorldPosition;

        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                int checkX = node.GridX + x;
                int checkY = node.GridY + y;

                if (IsValidGridCoordinate(checkX, checkY))
                {
                    PathNode2D candidate = grid[checkX, checkY];
                    if (candidate.IsWalkable)
                    {
                        float dist = (candidate.WorldPosition - nodePos).sqrMagnitude;
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            bestNode = candidate;
                        }
                    }
                }
            }
        }

        return bestNode ?? node;
    }

    public void RefreshNodeAtPosition(Vector3 worldPosition)
    {
        PathNode2D node = GetNodeFromWorldPosition(worldPosition);
        if (node != null)
        {
            node.IsWalkable = CheckPositionWalkable(node.WorldPosition);
        }
    }

    private bool IsValidGridCoordinate(int x, int y)
    {
        return x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY;
    }
    #endregion

    #region Debug Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(gridCenter, new Vector3(gridWorldSize.x, gridWorldSize.y, 0f));

        if (!showGizmos || grid == null) return;

        Vector3 focusPoint = gridCenter;
#if UNITY_EDITOR
        if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
        {
            focusPoint = UnityEditor.SceneView.currentDrawingSceneView.camera.transform.position;
        }
        else if (PlayerController.Instance != null)
        {
            focusPoint = PlayerController.Instance.transform.position;
        }
#endif

        float cubeSize = (nodeRadius * 2f) * 0.9f;
        Vector3 cubeDimensions = new Vector3(cubeSize, cubeSize, 0.1f);

        int minX = Mathf.Clamp(Mathf.FloorToInt((focusPoint.x - gizmoDrawRadius - worldBottomLeft.x) / nodeDiameter), 0, gridSizeX - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((focusPoint.x + gizmoDrawRadius - worldBottomLeft.x) / nodeDiameter), 0, gridSizeX - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt((focusPoint.y - gizmoDrawRadius - worldBottomLeft.y) / nodeDiameter), 0, gridSizeY - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt((focusPoint.y + gizmoDrawRadius - worldBottomLeft.y) / nodeDiameter), 0, gridSizeY - 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                PathNode2D node = grid[x, y];
                if (node == null) continue;

                if (!node.IsWalkable)
                {
                    // Ô cản vẽ màu đỏ nổi bật
                    Gizmos.color = new Color(1f, 0f, 0f, 0.45f);
                    Gizmos.DrawCube(node.WorldPosition, cubeDimensions);
                }
                else if (!onlyShowObstacles)
                {
                    // Ô đi được vẽ màu xanh lá nhạt
                    Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
                    Gizmos.DrawWireCube(node.WorldPosition, cubeDimensions);
                }
            }
        }
    }
    #endregion
}
