using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ xử lý thuật toán tìm đường A* 2D (A-Star Pathfinding Algorithm).
/// Tìm lộ trình ngắn nhất giữa 2 điểm bất kỳ trên bản đồ, né hoàn toàn sông và tường,
/// đồng thời tối ưu hóa đường đi thành các mốc chuyển hướng mượt mà (Waypoints).
/// </summary>
public class Pathfinding2D : MonoBehaviour
{
    public static Pathfinding2D Instance { get; private set; }

    [Header("Tham Chiếu Lưới Bản Đồ")]
    [SerializeField] private PathfindingGrid2D grid;

    [Header("Cấu Hình Tìm Đường")]
    [Tooltip("Cho phép quái vật di chuyển theo 8 hướng (kèm đường chéo) để đường đi tự nhiên hơn")]
    [SerializeField] private bool allowDiagonalMovement = true;

    [Tooltip("Giới hạn số bước duyệt tối đa để bảo vệ hiệu năng CPU khi không thể tìm thấy đường")]
    [SerializeField] private int maxSearchIterations = 1500;

    [Tooltip("Rút gọn đường đi (bỏ qua các ô thẳng hàng, chỉ giữ các mốc góc cua)")]
    [SerializeField] private bool simplifyPathWaypoints = true;

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

        if (grid == null)
        {
            grid = GetComponent<PathfindingGrid2D>() ?? Object.FindFirstObjectByType<PathfindingGrid2D>();
        }
    }

    /// <summary>
    /// Tìm đường đi A* từ vị trí xuất phát đến vị trí mục tiêu.
    /// Trả về danh sách các điểm mốc (Waypoints) ngắn nhất trong không gian thế giới.
    /// </summary>
    public List<Vector2> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
    {
        if (grid == null)
        {
            grid = PathfindingGrid2D.Instance ?? Object.FindFirstObjectByType<PathfindingGrid2D>();
            if (grid == null) return null;
        }

        PathNode2D startNode = grid.GetNodeFromWorldPosition(startWorldPos);
        PathNode2D targetNode = grid.GetNodeFromWorldPosition(targetWorldPos);

        if (startNode == null || targetNode == null) return null;

        // Nếu điểm xuất phát trùng điểm đích
        if (startNode == targetNode)
        {
            return new List<Vector2> { targetWorldPos };
        }

        // Nếu điểm đích nằm trên tường/vật cản, tự động tìm ô trống gần nhất để quái vẫn tiếp cận được
        if (!targetNode.IsWalkable)
        {
            targetNode = grid.GetClosestWalkableNode(targetNode, 5);
            if (targetNode == null || !targetNode.IsWalkable) return null;
        }

        // Nếu ô bắt đầu của quái đang bị kẹt trong cản, tìm ô trống gần nhất để thoát ra
        if (!startNode.IsWalkable)
        {
            startNode = grid.GetClosestWalkableNode(startNode, 4);
            if (startNode == null) return null;
        }

        List<PathNode2D> openSet = new List<PathNode2D> { startNode };
        HashSet<PathNode2D> closedSet = new HashSet<PathNode2D>();

        startNode.GCost = 0;
        startNode.HCost = CalculateDistanceCost(startNode, targetNode);
        startNode.ParentNode = null;

        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxSearchIterations)
        {
            iterations++;

            // Tìm node có FCost thấp nhất trong OpenSet
            PathNode2D currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || 
                   (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Đã chạm đích thành công -> Truy vết đường đi
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode, targetWorldPos);
            }

            // Duyệt tất cả các Node hàng xóm lân cận
            List<PathNode2D> neighbors = grid.GetNeighborNodes(currentNode, allowDiagonalMovement);
            foreach (PathNode2D neighbor in neighbors)
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int movementCostToNeighbor = currentNode.GCost + CalculateDistanceCost(currentNode, neighbor);
                bool isNotInOpenSet = !openSet.Contains(neighbor);

                if (movementCostToNeighbor < neighbor.GCost || isNotInOpenSet)
                {
                    neighbor.GCost = movementCostToNeighbor;
                    neighbor.HCost = CalculateDistanceCost(neighbor, targetNode);
                    neighbor.ParentNode = currentNode;

                    if (isNotInOpenSet)
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // Không tìm được đường đi hoàn chỉnh do bị cô lập hoàn toàn
        return null;
    }

    /// <summary>
    /// Lần ngược theo ParentNode từ đích về xuất phát để dựng lại đường đi hoàn chỉnh
    /// </summary>
    private List<Vector2> RetracePath(PathNode2D startNode, PathNode2D endNode, Vector3 finalTargetWorldPos)
    {
        List<PathNode2D> pathNodes = new List<PathNode2D>();
        PathNode2D currentNode = endNode;

        while (currentNode != startNode && currentNode != null)
        {
            pathNodes.Add(currentNode);
            currentNode = currentNode.ParentNode;
        }

        pathNodes.Reverse();

        if (simplifyPathWaypoints)
        {
            return SimplifyPath(pathNodes, finalTargetWorldPos);
        }
        else
        {
            List<Vector2> waypoints = new List<Vector2>();
            for (int i = 0; i < pathNodes.Count; i++)
            {
                waypoints.Add(pathNodes[i].WorldPosition);
            }
            if (waypoints.Count > 0)
            {
                waypoints[waypoints.Count - 1] = finalTargetWorldPos;
            }
            return waypoints;
        }
    }

    /// <summary>
    /// Lược bớt các điểm nằm trên cùng một đường thẳng, chỉ giữ lại các mốc góc cua
    /// để quái vật lướt mượt mà không bị khựng từng ô
    /// </summary>
    private List<Vector2> SimplifyPath(List<PathNode2D> pathNodes, Vector3 finalTargetWorldPos)
    {
        List<Vector2> waypoints = new List<Vector2>();
        if (pathNodes == null || pathNodes.Count == 0) return waypoints;

        Vector2 directionOld = Vector2.zero;

        for (int i = 0; i < pathNodes.Count - 1; i++)
        {
            Vector2 directionNew = new Vector2(
                pathNodes[i + 1].GridX - pathNodes[i].GridX,
                pathNodes[i + 1].GridY - pathNodes[i].GridY
            );

            // Khi hướng đi thay đổi (rẽ cua), ghi nhận đây là 1 Waypoint
            if (directionNew != directionOld)
            {
                waypoints.Add(pathNodes[i].WorldPosition);
                directionOld = directionNew;
            }
        }

        // Luôn bổ sung điểm đích cuối cùng chính xác của mục tiêu
        waypoints.Add(finalTargetWorldPos);

        return waypoints;
    }

    /// <summary>
    /// Tính toán chi phí khoảng cách chuẩn Octile giữa 2 ô (14 cho đường chéo, 10 cho đường thẳng)
    /// </summary>
    private int CalculateDistanceCost(PathNode2D nodeA, PathNode2D nodeB)
    {
        int distX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        int distY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        if (distX > distY)
        {
            return 14 * distY + 10 * (distX - distY);
        }
        return 14 * distX + 10 * (distY - distX);
    }

    /// <summary>
    /// Kiểm tra nhanh tầm nhìn trực diện giữa 2 điểm (Line of Sight) bằng tia quét hình tròn CircleCast
    /// Trả về true nếu đường đi thẳng hoàn toàn thông thoáng không vướng tường hoặc sông
    /// </summary>
    public static bool HasDirectLineOfSight(Vector2 startPos, Vector2 targetPos, LayerMask obstacleLayers, float checkRadius = 0.35f)
    {
        Vector2 diff = targetPos - startPos;
        float distance = diff.magnitude;
        if (distance <= 0.05f) return true;

        Vector2 direction = diff / distance;
        RaycastHit2D hit = Physics2D.CircleCast(startPos, checkRadius, direction, distance, obstacleLayers);

        return hit.collider == null || hit.collider.isTrigger;
    }
}

