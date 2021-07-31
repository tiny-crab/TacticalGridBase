using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UniRx;

public class Grid : MonoBehaviour
{
    public ReactiveProperty<Vector3Int> hoveredCell = new ReactiveProperty<Vector3Int>();
    public ReactiveProperty<Vector3Int> clickedCell = new ReactiveProperty<Vector3Int>();

    Datastore datastore;
    Prefabs prefabs;
    public Tilemap tiles;

    void Start() {
        datastore = this.GetComponent<Datastore>();
        prefabs = this.GetComponent<Prefabs>();
        tiles = datastore.activeTilemap;

        datastore.gridEvents.Receive<HoverEvent>().Subscribe(e => {
            if (e.cell == null) { return; }
            else {
                var prev = hoveredCell;
                hoveredCell.Value = e.cell;
            }
        });

        datastore.gridEvents.Receive<GridEvent>().Subscribe(e => {
            if (e.cell == null) { return; }
            if (e.action == GridActions.SPAWN_UNIT) {
                SpawnUnitAt(e.cell);
            } else if (e.action == GridActions.SELECT_UNIT) {
                SelectTileAt(e.cell);
            } else if (e.action == GridActions.SELECT_TILE) {
                SelectTileAt(e.cell);
            }
        });

        datastore.units.ObserveEveryValueChanged(x => x.Count).Subscribe(_ => {
            datastore.units.ToList().ForEach(entry => {
                entry.Value.transform.position = tiles.GetCellCenterWorld(entry.Key);
            });
        });
    }

    public void SelectTileAt(Vector3Int cell) {
        clickedCell.Value = cell;
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

    public bool TileExistsAt(Vector3Int cell) {
        return tiles.GetTile(cell) != null;
    }
}

public enum GridActions {
    SPAWN_UNIT,
    SELECT_UNIT,
    SELECT_TILE,
}