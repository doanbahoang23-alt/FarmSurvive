using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class FarmableArea : MonoBehaviour
{
    public static Tilemap ActiveFarmTilemap { get; private set; }

    private void OnEnable()
    {
        ActiveFarmTilemap = GetComponent<Tilemap>();
    }

    private void OnDisable()
    {
        if (ActiveFarmTilemap == GetComponent<Tilemap>())
        {
            ActiveFarmTilemap = null;
        }
    }
}