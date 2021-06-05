using System.Globalization;
using UnityEngine;
using UnityEngine.Tilemaps;
using UniRx;

public class Grid : MonoBehaviour
{
    public Tile baseTileObject;
    public Tile hoverTileObject;

    Vector3Int hoveredCell;

    Datastore datastore;
    Tilemap tiles;

    void Start() {
        datastore = this.GetComponent<Datastore>();
        tiles = datastore.activeTilemap;

        datastore.inquireEvents.Receive<HoverEvent>().Subscribe(e => {
            if (e.cell == null) { return; }
            else {
                var prev = hoveredCell;
                hoveredCell = e.cell;
                RefreshCell(hoveredCell);
                RefreshCell(prev);
            }
        });
    }

    void RefreshCell(Vector3Int cell) {
        if (!TileExistsAt(cell)) { return; } // never refresh a tile that has not already been instantiated in the grid elsewhere

        if (cell == hoveredCell) {
            tiles.SetTile(cell, hoverTileObject);
        } else {
            tiles.SetTile(cell, baseTileObject);
        }
    }

    bool TileExistsAt(Vector3Int cell) {
        return tiles.GetTile(cell) != null;
    }
}
