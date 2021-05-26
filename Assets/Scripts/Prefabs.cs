using System.Collections.Generic;
using UnityEngine;

public class Prefabs : MonoBehaviour {
    public GameObject gridPrefab;
    public List<GameObject> tilemapPrefabs;

    void Awake() {
        gridPrefab = Resources.Load<GameObject>("Prefabs/Grid");
        tilemapPrefabs = new List<GameObject>() {
            Resources.Load<GameObject>("Prefabs/Tilemaps/L-Shape"),
            Resources.Load<GameObject>("Prefabs/Tilemaps/Ring"),
        };
    }
}
