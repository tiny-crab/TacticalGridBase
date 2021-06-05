# TacticalGridBase

### Jun 4

The structure so far:

#### Scene

There are only two objects in the scene:
* Main Camera (default settings, albeit size enlarged to 8)
* God (game object with a single component: the God script)

The God script is for instantiating and orchestrating all of the other objects. Due to issues with collaborating on a scene in a [previous game jam](https://github.com/zakattak/GameJamMay2021), I reduced the number of objects in a scene and forced the rest to be created via the God script.

##### God

The God object will instantiate an empty Container object at runtime that will contain all (or at least, if it's performant) of the abstract objects / scripts that don't pertain to a specific GameObject / entity in the game space. For now, you can see these declared [here](https://github.com/zakattak/TacticalGridBase/blob/main/Assets/Scripts/God.cs#L14-L24):

```
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
_I'd like to write up a custom attribute to apply to each of the members that is loaded in this way. It would be nice to clean up this code and not have the Awake function be bloated with instantiating every abstract component in the whole game. But for now, it stays._

After these are instantiated, each of these large components has easy access to one another. This was intentional, although it feels incredibly wrong since it's somewhat like global access. However, the main dependency that any of these components have is on the Datastore. They each retrieve it through a simple `this.datastore = this.GetComponent<Datastore>();`. No need to write Singleton patterns in any way, this guarantees that all of the components share the same instance of the datastore.

#### Scripts

##### Datastore

The Datastore is intended to publicize all shared data.

Right now, that only consists of:
```
public Tilemap activeTilemap;

public MessageBroker inputEvents = new MessageBroker();
public MessageBroker inquireEvents = new MessageBroker();
```

The `activeTilemap` variable is somewhat self-explanatory. The God object instantiates the tilemap via a prefab, and passes it in the datastore at `God.Start()`. 

The two `MessageBroker`s are how input events are abstracted. I wanted to practice with multiple control schemes, and also release the tight coupling of Unity `Input` with other components. I intend on writing some debug menus that will nicely visualize the events that come through these message brokers for easy debugging. I hope that will be generalizable enough to carry forward to other projects (where I've really struggled with state and events).

A `MessageBroker` is similar to other UniRx Observables, but `MessageBroker`s aren't dedicated to a certain class. You can push whatever you want into it (unlike Observables and ReactiveProperties) and the subscribers choose what types of messages they want to listen to. For example, the Grid subscribes to any `HoverEvent`s published in the `inquireEvents` broker:

```
datastore.inquireEvents.Receive<HoverEvent>().Subscribe(e => {
    if (e.cell == null) { return; }
    else {
        var prev = hoveredCell;
        hoveredCell = e.cell;
        RefreshCell(hoveredCell);
        RefreshCell(prev);
    }
});
```
_I would really like to find out if I can process curr and prev message in the same Subscribe event, but I don't know if that's the case. I may write an extension on the MessageBroker to hold the most recent messages to help with this_

In any case, there are two `MessageBroker`s at present, one that handles `inputEvents` (pretty clear) and another that handles `inquireEvents` (less clear). 

`inputEvents` are intended to be clicks, button presses, hotkey presses, etc. These are moments where the player is showing **intent** to do an action in the game (even if it doesn't map to a direct **avatar action**, it could still be a click to open an info menu or something).

`inquireEvents` are intended to be hovers (via cursor or virtual cursor controlled via thumbstick), but also more extensible to show a user's soft focus. I don't have huge ideas for this one yet, but maybe holding down a finger on mobile would also send `inquireEvents`?

There's one type of event per `MessageBroker` at present.

```
// pushed into inputEvents
public class InputEvent { 
    public Vector3Int cell;
}

// pushed into inquireEvents
public class HoverEvent {
    public Vector3Int cell;
}
```

I would like to consider these events as request bodies. Any of the members could be null, and the subscribers will parse out the behavior they want by null-checking the members they care about. Likely there will be more than these Events.

For now, since the only subscriber of these events is the `Grid` class, there are only tile Cell data that are tied to clicks and hovers. Menus and other GameObjects that sit above the Tilemap will end up adding new fields into these events (or creating their own). 

I decided not to have these events inherit from a single parent class `Event` for flexibility. 🤷 We'll see how that plays out.

#### Grid / Tilemap

Tilemaps are... something. And not clearly documented online.

There are four components to Tilemaps (from top to bottom):
* Grid
* The tilemap itself
* Tile palettes
* Tile objects

The Grid is not too special. Just set it to the right projection you want (rectangular, hex, isometric).

The tilemaps can be split into Prefabs, but they render weirdly in the Prefab viewer...

![image](https://user-images.githubusercontent.com/8145874/120882331-c9b2db00-c58b-11eb-80a4-acc344d5cc74.png)

There are many tutorials online how to set up Tilemaps / tile palettes / tile objects such as [these](https://www.youtube.com/results?search_query=tilemaps+unity).

There are fewer tutorials about how to interact with Tile objects in scripts and work with them interactively.

##### Cells

The tilemap is split into its own grid coordinates separate from Unity's grid units. When you're using non-rectangular grids, the tiles won't line up.

You need to switch any World coordinates into cell coordinates like [so](https://github.com/zakattak/TacticalGridBase/blob/main/Assets/Scripts/Mouse.cs#L35-L38):
```
Vector3Int GetMouseCellPosition() {
    var cellPoint = datastore.activeTilemap.WorldToCell(Camera.main.ScreenToWorldPoint(Input.mousePosition));
    return new Vector3Int(cellPoint.x, cellPoint.y, 0);
}
```
The `WorldToCell` method gives you back 2D int coordinates for the floating point world coordinates you're working with. This actually makes working with non-rectangular grids quite simple! No weird math for dealing with isometric tiling.
_Side note: for whatever reason, my mouse position would always end up on `z: -10`. I forced the output of this function to `z: 0` to compensate._

##### Tiles

When you make a tilemap and load it into the game, the tilemap will hold an array of every cell in the smallest rectangular region that encompasses your shape. In the case of my L-shaped grid, that comes to 19x14 (or 266 tiles), in which a number of them are null. 

I ran into trouble trying to read these tiles from a TilemapCollider2D component I placed on the top-level Tilemap. This constructs a single collider dynamically resized to the shape of your contiguous tile regions. Cool for a platformer, but not helpful for a tactical game, because we want to be able to detect collisions with any tile on the grid.

However, there isn't (as far as I can tell) a way to attach colliders to Tile objects. In fact, you can't attach any components to Tile objects. On top of that, you can't add Tile objects as components to other GameObjects. So in a sense, the only thing that can really manage these Tile objects is the Tilemap itself.

You're forced to use the [Tilemap API](https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap.html) to make changes to these Tile objects, which includes their color. My first task with this change was to add a different color on hover. Here's the process to unlock the Tile objects so that they can be recolored (a process that I didn't even end up using, but was cryptic anywhere online).

Go to the Tile object directory that you created when making a tile palette:

![image](https://user-images.githubusercontent.com/8145874/120882712-68d8d200-c58e-11eb-83d6-5ccd9ee7946a.png)

Click on the Tile you want to unlock (I selected all of mine) and navigate to the Inspector pane:

![image](https://user-images.githubusercontent.com/8145874/120882732-827a1980-c58e-11eb-9709-275a43725207.png)

Click on the triple-dot menu and select 'Debug':

![image](https://user-images.githubusercontent.com/8145874/120882768-ad646d80-c58e-11eb-8132-1aafd4a608fd.png)

The inspector window should change what fields are visible, and change the 'Flags' dropdown to 'None'.

![image](https://user-images.githubusercontent.com/8145874/120882787-c1a86a80-c58e-11eb-809d-58ed3ed7029c.png)

Now that the Tile objects are unlocked, the Tilemap API is a bit more usable. God knows why it's set up like this.

#### Manipulating Tiles

My first mistake was to think of Tiles as GameObjects. They are not. They have very little information that is similar to GameObjects, and it best to avoid working with them at all. 

Instead, operate on the level of cells, and only dip into Tile data when absolutely necessary. I only need to do it [here](https://github.com/zakattak/TacticalGridBase/blob/main/Assets/Scripts/Grid.cs#L32), to identify if a tile was set in the tilemap or not:

```
void RefreshCell(Vector3Int cell) {
    if (!TileExistsAt(cell)) { return; } // never refresh a tile that has not already been instantiated in the grid elsewhere

    // ... more code
}

bool TileExistsAt(Vector3Int cell) {
    return tiles.GetTile(cell) != null;
}
```

To change the tile while hovering over it, I used `SetTile` (I couldn't get `SetColor` to work, and besides, that only adds a tint on top of the Tile sprite instead of manipulating it altogether). Unfortunately, since Tiles are not GameObjects nor components, I was stuck with serializing some fields and dropped the Tile objects in through the inspector.

```
public class Grid : MonoBehaviour
{
    public Tile baseTileObject;
    public Tile hoverTileObject;

    // ... more code
}
```

![image](https://user-images.githubusercontent.com/8145874/120882980-8e1a1000-c58f-11eb-95f8-343c6c8ef912.png)

Then, when the Grid component receives a `HoverEvent`, it will update the respective tile and "de-update" the tile that is no longer being hovered over.

```
datastore.inquireEvents.Receive<HoverEvent>().Subscribe(e => {
    if (e.cell == null) { return; }
    else {
        var prev = hoveredCell;
        hoveredCell = e.cell;
        RefreshCell(hoveredCell);
        RefreshCell(prev);
    }
});

void RefreshCell(Vector3Int cell) {
    // ...

    if (cell == hoveredCell) {
        tiles.SetTile(cell, hoverTileObject);
    } else {
        tiles.SetTile(cell, baseTileObject);
    }
}
```

It would be nice to avoid storing the `hoveredCell` in state. I contemplated attaching a subscriber to each Tilemap cell, and updating if the `HoverEvent` contained its coordinate. I would need to test its performance, however, and it feels like overkill to run (in my case) 266 different threads just to see if one tile has updated. 

On top of that, I thought it best to not set up a subscriber for each event type, because then it would not have the context of any other input events that have been generated. What if the user is hovering over an enemy after clicking on one of their characters? What if they are hovering over a tile in the move range of an enemy? Tiles need to know the full context of functions that are applying upon them, and so having a myopic view of a single stream of events at a time didn't seem the way to go. This code block will likely end up a giant switch statement for how to render the context properly. But... [I've had experience with that before](https://github.com/zakattak/Timbre/blob/48f47f20739aaea423d09ce7ca7899cbd58e3397/Assets/Scripts/Grid/System/Component/Entity/Tile.cs#L106).

#### Visuals and Reusability
I'm trying to have some reusable resources across projects. The Utils script and some basic sprites in the [Solarized Dark colors](https://gist.github.com/ninrod/b0f86d77ebadaccf7d9d4431dd8e2983) I intend on carrying forward. I also hope to extract what I can from the MessageBrokers and input event handlers so that it can be easily dropped into new projects and I've already built up familiarity with them.
