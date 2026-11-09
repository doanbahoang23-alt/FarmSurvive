using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    private Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadMap(string mapName)
    {
        SceneManager.LoadScene(mapName, LoadSceneMode.Additive);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint")
                             ?? GameObject.Find("PlayerSpawn")
                             ?? GameObject.FindGameObjectWithTag("Respawn");
        if (spawnPoint != null && player != null)
        {
            player.position = spawnPoint.transform.position;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
