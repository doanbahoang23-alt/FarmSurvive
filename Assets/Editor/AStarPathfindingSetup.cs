#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Công cụ Editor hỗ trợ thiết lập hệ thống Tìm đường A* 2D chỉ trong 1 Click chuột.
/// </summary>
public static class AStarPathfindingSetup
{
    [MenuItem("Tools/FarmSurvive/Cấu Hình Hệ Thống Tìm Đường A* 2D (Setup AStar Pathfinding)", false, 30)]
    public static void SetupAStarPathfinding()
    {
        // 1. Kiểm tra xem trong Scene đã có GameObject quản lý A* chưa
        PathfindingGrid2D existingGrid = Object.FindFirstObjectByType<PathfindingGrid2D>(FindObjectsInactive.Include);
        Pathfinding2D existingPathfinding = Object.FindFirstObjectByType<Pathfinding2D>(FindObjectsInactive.Include);

        GameObject aStarObj = null;

        if (existingGrid != null)
        {
            aStarObj = existingGrid.gameObject;
        }
        else if (existingPathfinding != null)
        {
            aStarObj = existingPathfinding.gameObject;
        }
        else
        {
            aStarObj = new GameObject("[AStar_Pathfinding]");
            Undo.RegisterCreatedObjectUndo(aStarObj, "Create [AStar_Pathfinding]");
        }

        // 2. Gắn và cấu hình PathfindingGrid2D
        PathfindingGrid2D gridComp = aStarObj.GetComponent<PathfindingGrid2D>();
        if (gridComp == null)
        {
            gridComp = Undo.AddComponent<PathfindingGrid2D>(aStarObj);
        }

        // Tự động căn chỉnh kích thước lưới theo Tilemap trong Scene
        gridComp.FitGridToSceneTilemaps();
        gridComp.BuildGrid();

        // 3. Gắn và cấu hình Pathfinding2D
        Pathfinding2D pathComp = aStarObj.GetComponent<Pathfinding2D>();
        if (pathComp == null)
        {
            pathComp = Undo.AddComponent<Pathfinding2D>(aStarObj);
        }

        EditorUtility.SetDirty(aStarObj);
        Selection.activeGameObject = aStarObj;

        Debug.Log($"<color=#55FF55><b>[AStar Pathfinding]</b> Đã thiết lập thành công hệ thống Tìm Đường A* 2D trên GameObject '{aStarObj.name}'!</color>\n" +
                  $"Kích thước lưới: {gridComp.GridWorldSize}, Tâm lưới: {gridComp.GridCenter}, Số ô: {gridComp.GridSizeX}x{gridComp.GridSizeY}.");
    }
}
#endif

