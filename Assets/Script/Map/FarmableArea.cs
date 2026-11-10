using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class FarmableArea : MonoBehaviour
{
    public static Tilemap ActiveFarmTilemap { get; private set; }

    private static Tilemap[] cachedBridgeTilemaps;
    private static float lastBridgeCacheTime = -10f;

    private static Tilemap[] cachedWaterTilemaps;
    private static float lastWaterCacheTime = -10f;

    private void OnEnable()
    {
        ActiveFarmTilemap = GetComponent<Tilemap>();
        CleanupInvalidTiles();
    }

    private void Start()
    {
        CleanupInvalidTiles();
    }

    private void OnDisable()
    {
        if (ActiveFarmTilemap == GetComponent<Tilemap>())
        {
            ActiveFarmTilemap = null;
        }
    }

    /// <summary>
    /// Dọn sạch các ô đất cuốc vô tình đặt trên mặt cầu hoặc trên nước khi nạp game hoặc bắt đầu cảnh
    /// </summary>
    public void CleanupInvalidTiles()
    {
        Tilemap farmTilemap = GetComponent<Tilemap>();
        if (farmTilemap == null) return;

        BoundsInt bounds = farmTilemap.cellBounds;
        if (bounds.size.x == 0 || bounds.size.y == 0) return;

        List<Vector3Int> tilesToRemove = new List<Vector3Int>();

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!farmTilemap.HasTile(pos)) continue;

            Vector3 worldCenter = farmTilemap.GetCellCenterWorld(pos);
            if (!CanTillAt(worldCenter, worldCenter))
            {
                tilesToRemove.Add(pos);
            }
        }

        for (int i = 0; i < tilesToRemove.Count; i++)
        {
            farmTilemap.SetTile(tilesToRemove[i], null);
        }
    }

    public static Tilemap[] GetBridgeTilemaps()
    {
        if (cachedBridgeTilemaps == null || Time.time - lastBridgeCacheTime > 2f)
        {
            lastBridgeCacheTime = Time.time;
            Tilemap[] allMaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            List<Tilemap> bridges = new List<Tilemap>();
            int buildingLayer = LayerMask.NameToLayer("Building");

            for (int i = 0; i < allMaps.Length; i++)
            {
                Tilemap tm = allMaps[i];
                if (tm == null) continue;
                if (tm.GetComponent<FarmableArea>() != null) continue;

                string nameLower = tm.gameObject.name.ToLower();
                if (nameLower.Contains("soil") || nameLower.Contains("farm")) continue;

                bool isBridgeName = nameLower.Contains("bridge");
                bool isBridgeTag = tm.CompareTag("Bridge");
                bool isBuildingLayer = (buildingLayer != -1 && tm.gameObject.layer == buildingLayer);

                if (isBridgeName || isBridgeTag || isBuildingLayer)
                {
                    bridges.Add(tm);
                }
            }

            cachedBridgeTilemaps = bridges.ToArray();
        }

        return cachedBridgeTilemaps;
    }

    public static Tilemap[] GetWaterTilemaps()
    {
        if (cachedWaterTilemaps == null || Time.time - lastWaterCacheTime > 2f)
        {
            lastWaterCacheTime = Time.time;
            Tilemap[] allMaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            List<Tilemap> waters = new List<Tilemap>();
            int waterLayer = LayerMask.NameToLayer("Water");

            for (int i = 0; i < allMaps.Length; i++)
            {
                Tilemap tm = allMaps[i];
                if (tm == null) continue;
                if (tm.GetComponent<FarmableArea>() != null) continue;

                string nameLower = tm.gameObject.name.ToLower();
                if (nameLower.Contains("soil") || nameLower.Contains("farm")) continue;

                bool isWaterName = nameLower.Contains("water");
                bool isWaterLayer = (waterLayer != -1 && tm.gameObject.layer == waterLayer);

                if (isWaterName || isWaterLayer)
                {
                    waters.Add(tm);
                }
            }

            cachedWaterTilemaps = waters.ToArray();
        }

        return cachedWaterTilemaps;
    }

    public static bool IsOnBridge(Vector3 worldPos)
    {
        Tilemap[] bridges = GetBridgeTilemaps();
        if (bridges == null || bridges.Length == 0) return false;

        for (int i = 0; i < bridges.Length; i++)
        {
            Tilemap tm = bridges[i];
            if (tm == null || !tm.gameObject.activeInHierarchy) continue;

            if (tm.HasTile(tm.WorldToCell(worldPos))) return true;

            // Kiểm tra các điểm lệch xung quanh để tránh lệch biên grid giữa FarmSoil và Bridge
            float delta = 0.35f;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(delta, 0, 0)))) return true;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(-delta, 0, 0)))) return true;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(0, delta, 0)))) return true;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(0, -delta, 0)))) return true;
        }

        return false;
    }

    public static bool IsOnWater(Vector3 worldPos)
    {
        Tilemap[] waters = GetWaterTilemaps();
        if (waters == null || waters.Length == 0) return false;

        for (int i = 0; i < waters.Length; i++)
        {
            Tilemap tm = waters[i];
            if (tm == null || !tm.gameObject.activeInHierarchy) continue;

            if (tm.HasTile(tm.WorldToCell(worldPos))) return true;

            float delta = 0.35f;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(delta, 0, 0)))) return true;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(-delta, 0, 0)))) return true;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(0, delta, 0)))) return true;
            if (tm.HasTile(tm.WorldToCell(worldPos + new Vector3(0, -delta, 0)))) return true;
        }

        return false;
    }

    public static bool CanTillAt(Vector3 centerPos, Vector3 targetPos)
    {
        if (IsOnBridge(centerPos) || IsOnBridge(targetPos)) return false;
        if (IsOnWater(centerPos) || IsOnWater(targetPos)) return false;
        return true;
    }
}
