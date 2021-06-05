# TacticalGridBase

### Jun 4

```
// Using this style to update tiles for two reasons:
//   1. there's a lot of tiles in a grid, so don't want to create a subscriber for each one
//   2. this will handle logic about how a tile looks when there are a plethora of different functions applied to it
//      i.e. move range, attack range, clicked, hovered, enemy, ally, etc.
//      it will be easier if this is all in one function, where a tile can check if it is hovered, clicked, etc.
void RefreshCell(Vector3Int cell) {
    if (!TileExistsAt(cell)) { return; } // never refresh a tile that has not already been instantiated in the grid elsewhere

    if (cell == hoveredCell) {
        tiles.SetTile(cell, hoverTileObject);
    } else {
        tiles.SetTile(cell, baseTileObject);
    }
}
```

```
    // I'd like to wrap these params in some kind of dependency injection annotations
    // Although the injector would just be creating a container object and calling add comp / get comp
    // it would be cleaner as more of these components are created, instead of listing and bloating the constructor
    // there are 2 extra lines for each of these components
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
```