using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UniRx;

public class Grid : MonoBehaviour
{
    public Tile baseTileObject;
    public Tile hoverTileObject;
    public Tile clickTileObject;

    Vector3Int hoveredCell;
    Vector3Int clickedCell;

    Datastore datastore;
    Prefabs prefabs;
    Tilemap tiles;

    void Start() {
        datastore = this.GetComponent<Datastore>();
        prefabs = this.GetComponent<Prefabs>();
        tiles = datastore.activeTilemap;

        datastore.gridEvents.Receive<HoverEvent>().Subscribe(e => {
            if (e.cell == null) { return; }
            else {
                var prev = hoveredCell;
                hoveredCell = e.cell;
                RefreshCell(hoveredCell);
                RefreshCell(prev);
            }
        });

        datastore.gridEvents.Receive<InputEvent>().Subscribe(e => {
            if (e.cell == null) { return; }
            var prev = clickedCell;
            clickedCell = e.cell;
            RefreshCell(clickedCell);
            RefreshCell(prev);
        });

        datastore.gridEvents.Receive<GridEvent>().Subscribe(e => {
            if (e.cell == null) { return; }
            if (e.action == GridActions.SPAWN_UNIT) {
                SpawnUnitAt(e.cell);
            }
        });

        datastore.units.ObserveEveryValueChanged(x => x.Count).Subscribe(_ => {
            datastore.units.ToList().ForEach(entry => {
                entry.Value.transform.position = tiles.GetCellCenterWorld(entry.Key);
            });
        });
    }

    public void SpawnUnitAt(Vector3Int cell, GameObject unit = null) {
        if (datastore.units.ContainsKey(cell) || !TileExistsAt(cell)) {
            return;
        } else {
            if (unit == null) {
                unit = GameObject.Instantiate(prefabs.baseUnitPrefab);
                var unitColorPool = new List<Utils.SolarizedColors>() {
                    Utils.SolarizedColors.red, Utils.SolarizedColors.blue,
                };
                var randomColor = Utils.solColors[unitColorPool.getRandomElement()];
                unit.GetComponent<SpriteRenderer>().color = new Color(randomColor.r, randomColor.g, randomColor.b, 1);
            }
            datastore.units[cell] = unit.gameObject;
        }
    }

    void RefreshCell(Vector3Int cell) {
        if (!TileExistsAt(cell)) { return; } // never refresh a tile that has not already been instantiated in the grid elsewhere

        if (cell == hoveredCell) {
            tiles.SetTile(cell, hoverTileObject);
        } else if (cell == clickedCell) {
            tiles.SetTile(cell, clickTileObject);
        }else {
            tiles.SetTile(cell, baseTileObject);
        }
    }

    bool TileExistsAt(Vector3Int cell) {
        return tiles.GetTile(cell) != null;
    }
}

public enum GridActions {
    SPAWN_UNIT,
}
