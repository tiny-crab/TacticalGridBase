using UnityEngine;
using UniRx;
using System.Collections.Generic;
using System.Linq;

class GridRenderer : MonoBehaviour {
    Grid grid;
    Prefabs prefabs;
    Datastore datastore;

    class RenderState {
        public Vector3Int hoveredCell;
        public List<Vector3Int> selectedCells = new List<Vector3Int>();

        // replace with `record` and `with` when C# 9.0 is supported by Unity 🙃
        public RenderState Clone() {
            return new RenderState() {
                hoveredCell = hoveredCell,
                selectedCells = selectedCells
            };
        }
    };
    RenderState currentRender = new RenderState();
    List<RenderState> rendersToApply = new List<RenderState>();

    public List<TileRender> texturedTiles = new List<TileRender>();

    void Start() {
        grid = this.GetComponent<Grid>();
        prefabs = this.GetComponent<Prefabs>();
        datastore = this.GetComponent<Datastore>();

        grid.hoveredCell
            .Where(cell => grid.TileExistsAt(cell))
            .Subscribe(cell => {
                var newRender = currentRender.Clone();
                newRender.hoveredCell = cell;
                rendersToApply.Add(newRender);
            });

        grid.clickedCell
            .Where(cell => grid.TileExistsAt(cell))
            .Subscribe(cell => {
                var selectedCells = new List<Vector3Int>();

                // highlight move range when clicking on unit
                if (datastore.units.ContainsKey(cell)) {
                    // highlight all move range cells
                    var unit = datastore.units[cell].GetComponent<Unit>();
                    for (var x = cell.x - unit.moveRange; x <= cell.x + unit.moveRange; x++) {
                        for (var y = cell.y - unit.moveRange; y <= cell.y + unit.moveRange; y++) {
                            var lookAtCell = new Vector3Int(x, y, 0);
                            var distFromUnit = Mathf.Abs(cell.x - x) + Mathf.Abs(cell.y - y);
                            if (grid.TileExistsAt(lookAtCell) && distFromUnit <= unit.moveRange) {
                                selectedCells.Add(lookAtCell);
                            }
                        }
                    }
                } else {
                    selectedCells.Add(cell);
                }

                var newRender = currentRender.Clone();
                newRender.selectedCells = selectedCells;
                rendersToApply.Add(newRender);
            });
    }

    void Update() {
        if (rendersToApply.Count > 0) { Render(); }
    }

    void Render() {
        rendersToApply.ForEach(newRender => {
            if (currentRender.hoveredCell != newRender.hoveredCell) {
                if (grid.TileExistsAt(currentRender.hoveredCell)
                    && !currentRender.selectedCells.Contains(newRender.hoveredCell)
                ) {
                    // this prevents tile at (0,0,0) from appearing if it didn't exist already
                    grid.tiles.SetTile(currentRender.hoveredCell, prefabs.baseTileObject);
                    grid.tiles.SetTile(newRender.hoveredCell, prefabs.hoverTileObject);
                }
            }

            if (!currentRender.selectedCells.Equals(newRender.selectedCells)) {
                currentRender.selectedCells.ForEach(cell => {
                    grid.tiles.SetTile(cell, prefabs.baseTileObject);
                });
                newRender.selectedCells.ForEach(cell => {
                    grid.tiles.SetTile(cell, prefabs.clickTileObject);
                });
            }

            currentRender = newRender;
        });

        rendersToApply = new List<RenderState>();
    }
}

public enum TileBaseTexture {
    BASE,
    CLICK,
    HOVER
}

public class TileRender {
    public Vector3Int cell;
    public List<TileBaseTexture> appliedTextures;

    public TileBaseTexture GetVisibleTexture() {
        return appliedTextures.OrderBy(tileType => (int) tileType).First();
    }
}