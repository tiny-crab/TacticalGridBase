using UnityEngine;
using UnityEngine.Tilemaps;

public class God : MonoBehaviour {
    GameObject container;

    Prefabs prefabs;
    Datastore datastore;
    Mouse mouse;
    Grid grid;

    void Awake() {
        var container = new GameObject();
        container.name = "Container";

        container.AddComponent<Prefabs>();
        container.AddComponent<Datastore>();
        container.AddComponent<Mouse>();
        container.AddComponent<Grid>();

        prefabs = container.GetComponent<Prefabs>();
        datastore = container.GetComponent<Datastore>();
        mouse = container.GetComponent<Mouse>();
        grid = container.GetComponent<Grid>();
    }

    void Start() {
        var grid = GameObject.Instantiate(prefabs.gridPrefab);
        var activeTilemap = GameObject.Instantiate(prefabs.tilemapPrefabs[1]);
        activeTilemap.transform.SetParent(grid.transform);

        datastore.activeTilemap = activeTilemap.GetComponent<Tilemap>();
    }
}
