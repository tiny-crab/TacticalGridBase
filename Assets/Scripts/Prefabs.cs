using System.Collections.Generic;
using UnityEngine;

public class Prefabs : MonoBehaviour {

    public GameObject gridPrefab;
    public List<GameObject> tilemapPrefabs;
    public GameObject baseUnitPrefab;

    // UI
    public GameObject canvasPrefab;
    public GameObject debugMenuPrefab;

    void Awake() {
        gridPrefab = Resources.Load<GameObject>("Prefabs/Grid");
        tilemapPrefabs = new List<GameObject>() {
            Resources.Load<GameObject>("Prefabs/Tilemaps/L-Shape"),
            Resources.Load<GameObject>("Prefabs/Tilemaps/Ring"),
        };
        baseUnitPrefab = Resources.Load<GameObject>("Prefabs/Units/BaseUnit");

        canvasPrefab = Resources.Load<GameObject>("Prefabs/UI/ParentCanvas");
        debugMenuPrefab = Resources.Load<GameObject>("Prefabs/UI/DebugDropdown");
    }
}
