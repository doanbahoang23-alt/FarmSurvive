using UnityEngine;

/// <summary>
/// Đại diện cho 1 ô (Node) trên lưới tìm đường A* 2D.
/// Lưu trữ tọa độ lưới, vị trí thế giới, trạng thái đi được và các chỉ số chi phí (G, H, F Cost).
/// </summary>
public class PathNode2D
{
    /// <summary> Tọa độ cột trên lưới (X) </summary>
    public int GridX { get; set; }

    /// <summary> Tọa độ hàng trên lưới (Y) </summary>
    public int GridY { get; set; }

    /// <summary> Tọa độ thực tế trong không gian thế giới 2D </summary>
    public Vector3 WorldPosition { get; set; }

    /// <summary> Ô này có đi qua được không (True = Đất trống, False = Tường/Sông/Vật cản) </summary>
    public bool IsWalkable { get; set; }

    /// <summary> Chi phí di chuyển từ điểm xuất phát (Start) đến Node này (G Cost) </summary>
    public int GCost { get; set; }

    /// <summary> Chi phí ước tính từ Node này đến đích (Target) dựa trên khoảng cách (H Cost - Heuristic) </summary>
    public int HCost { get; set; }

    /// <summary> Tổng chi phí đánh giá của Node (F Cost = G Cost + H Cost) </summary>
    public int FCost => GCost + HCost;

    /// <summary> Node cha liền trước để phục vụ truy vết ngược lại đường đi hoàn chỉnh </summary>
    public PathNode2D ParentNode { get; set; }

    public PathNode2D(bool isWalkable, Vector3 worldPosition, int gridX, int gridY)
    {
        IsWalkable = isWalkable;
        WorldPosition = worldPosition;
        GridX = gridX;
        GridY = gridY;
        GCost = 0;
        HCost = 0;
        ParentNode = null;
    }
}

