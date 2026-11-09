using UnityEngine;

[CreateAssetMenu(fileName = "NewWaveData", menuName = "KnightGame/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Infinite Wave Settings")]
    public GameObject enemyPrefab;
    public int baseEnemyCount = 1;
    public float spawnDelay = 1f;
    public float timeBetweenWaves = 30f;
}