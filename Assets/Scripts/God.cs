using UnityEngine;
using UnityEngine.Tilemaps;

public class God : MonoBehaviour {
    GameObject container;

    Prefabs prefabs;
    Datastore datastore;
    Mouse mouse;

    void Awake() {
        var container = new GameObject();
        container.name = "Container";

        container.AddComponent<Prefabs>();
        container.AddComponent<Datastore>();
        container.AddComponent<Mouse>();

        prefabs = container.GetComponent<Prefabs>();
        datastore = container.GetComponent<Datastore>();
        mouse = container.GetComponent<Mouse>();
    }

    void Start() {
        var grid = GameObject.Instantiate(prefabs.gridPrefab);
        var activeTilemap = GameObject.Instantiate(prefabs.tilemapPrefabs[0]);
        activeTilemap.transform.SetParent(grid.transform);

        datastore.activeTilemap = activeTilemap.GetComponent<Tilemap>();
    }
}
