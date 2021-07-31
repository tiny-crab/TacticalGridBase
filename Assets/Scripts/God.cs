using UnityEngine;
using UnityEngine.Tilemaps;

public class God : MonoBehaviour {

    Prefabs prefabs;
    Datastore datastore;

    void Awake() {
        this.gameObject.AddComponent<Prefabs>();
        this.gameObject.AddComponent<Datastore>();
        this.gameObject.AddComponent<Mouse>();
        this.gameObject.AddComponent<Grid>();
        this.gameObject.AddComponent<UI>();
        this.gameObject.AddComponent<DebugTools>();
        this.gameObject.AddComponent<StateMachine>();
        this.gameObject.AddComponent<GridRenderer>();

        prefabs = this.gameObject.GetComponent<Prefabs>();
        datastore = this.gameObject.GetComponent<Datastore>();
    }

    void Start() {
        var grid = GameObject.Instantiate(prefabs.gridPrefab);
        var activeTilemap = GameObject.Instantiate(prefabs.tilemapPrefabs[0]);
        activeTilemap.transform.SetParent(grid.transform);

        datastore.activeTilemap = activeTilemap.GetComponent<Tilemap>();
    }
}
