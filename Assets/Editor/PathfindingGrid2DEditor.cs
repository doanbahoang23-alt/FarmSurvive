#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector cho PathfindingGrid2D, cho phép dev căn chỉnh kích thước và xây dựng lưới trực tiếp từ Editor.
/// </summary>
[CustomEditor(typeof(PathfindingGrid2D))]
public class PathfindingGrid2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ inspector mặc định
        DrawDefaultInspector();

        PathfindingGrid2D grid = (PathfindingGrid2D)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Công Cụ Điều Khiển Lưới (Grid Controls)", EditorStyles.boldLabel);

        // Nút 1: Tự động căn chỉnh theo Tilemaps
        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("📐 Căn Chỉnh Theo Tilemaps (Fit To Tilemaps)", GUILayout.Height(32)))
        {
            Undo.RecordObject(grid, "Fit Grid To Tilemaps");
            grid.FitGridToSceneTilemaps();
            EditorUtility.SetDirty(grid);
            Debug.Log($"<color=#55FF55><b>[PathfindingGrid2D]</b> Đã tự động căn chỉnh lưới theo Tilemaps trong Scene:\n" +
                      $"Size = {grid.GridWorldSize}, Center = {grid.GridCenter}</color>");
        }

        // Nút 2: Xây dựng / Cập nhật lại lưới
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("🔨 Xây Dựng / Cập Nhật Lưới (Build / Rebuild Grid)", GUILayout.Height(32)))
        {
            Undo.RecordObject(grid, "Build Grid");
            grid.BuildGrid();
            EditorUtility.SetDirty(grid);
            Debug.Log($"<color=#55FF55><b>[PathfindingGrid2D]</b> Đã xây dựng lưới thành công: {grid.GridSizeX}x{grid.GridSizeY} ô ({grid.GridSizeX * grid.GridSizeY} nodes).</color>");
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif

