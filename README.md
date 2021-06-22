# TacticalGridBase

### Jun 4 - ["Add mouse hover over tile" commit](https://github.com/zakattak/TacticalGridBase/commit/acdf3a63b3a51a94fed1fba8983a812659e76873)

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

#### End Result
![Ring Tilemap](https://user-images.githubusercontent.com/8145874/120908109-00d0cd00-c61c-11eb-8a94-d730a919ffe3.gif)

![L-shape Tilemap](https://user-images.githubusercontent.com/8145874/120908134-3675b600-c61c-11eb-8e5f-48037447329f.gif)

### June 21 - [Debug Tools and Spawning Units](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751)

First: a gif to show what this commit introduces.

![TacticalGridBase_DebugTools1](https://user-images.githubusercontent.com/8145874/122863368-7eacfd80-d2d7-11eb-8419-6cad80883814.gif)

#### Minor Changes

Here's a couple of minor things I modified, just to clear up the diff for this commit!

* The God object no longer instantiates an empty `Container` object to place all body-less scripts. It just adds them directly to the God object itself. [Here's the diff](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751)
* The joy of converting all the color codes into [floating fractions](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-50aaa2f566317cb905af7ec1ff601f530755473e64ead8856617cbfe094924cdL12-R28) :simple_smile:.
* I modified the Event classes to contain a `publisher` member for debugging down the road. I did this now instead of later because I knew I would eventually want it, and I didn't want to have to change all instances of new Event constructors. [Here](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-6e93b4a3ea58e2ac54e6b4402cd4097fdb1f4a35dbe09490693fb9ca32365d6aL3-R11) is the underlying Event change and [here](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-c1e87332dc5a93c8889accd99ae1768fb589fd9ab7d9e046dd527b12e850e2f4L17-R20) is an example diff of how I changed the constructors.
* Added some new prefabs, namely for UI and units for spawning. Not much to talk about [here](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-5e42e17e768ce3756fcffc68481f8307f188135dca2788dd43e80639ce8dd696L4-L15).

#### Structural Changes

There are no major structural rewirings across the game code. There are a couple of new components that I added:
* [UI](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-8d19c394398e33e84468840bf673c14f298c2207c18182e5128198fe96d4cfb6R9), to display the currently selected DebugTool and whether or not to use it
* [StateMachine](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-81ec46ac9ca255dc3e2e6105d161f2f7479c75f8fac471d80d3dd09bfa451912R4), to contain game logic concerning how to transmute Input/InquireEvents into GridEvents (more on that later)
* [DebugTools](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-152987b11edb9444e6ab972b00b8be5fe89c1b2b60c34f6efca255c9e51f2a91R6), a class that is intended to hold "overriding" behavior for tools and debugging views (more on that, again, later)
* [GridActions](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-80c005a15d3fdcb613a2499ee9d9f1b1ec283721bf7fb2e476e1de98afe85fceR90), a very simple enum (for now) that will look a bit like an operation on a table!

Let's first start with the architectural problem I encountered when thinking about how to implement DebugTools.

#### Event Streams and Processing

I opted to decouple my major game components with the use of event streaming. Instead of calling operations directly from component to component, any component can publish a message (the event) to a `MessageBroker` (the stream). The publisher and receiver have little to no knowledge of each other. 

What I find attractive about event streams in this instance is that I can start to use my mental model of APIs and services as a way to express my understanding of the game grid and what can operate on it.

We can define the game grid as simply as:
* The game grid holds the positions of all of the units in play

Then, the two other components that want to operate on it can be defined as:
* The state machine filters / processes user input according to the rules of the game, and makes operations on the grid as such
* The debug tools allow a developer to completely circumvent the rules of the game, and make operations on the grid at will

As an example, consider a board game. It is made up of all the pieces in the box, and the rules with which to play with them. The pieces in the box exist in physical, real space. The rules do not. The rules exist in a virtual, "meta" space (the players' minds). Because the pieces in the box exist in a wholly different plane than the rules do, they are not coupled. Anybody interacting with the board game can choose to play the game by learning the rules, and applying them to the physical pieces. They can alternatively forgo all the rules, and play with the pieces however they please.

(Really, if its not clear already, this project is mostly a way for me to mentally model and reason about all of these different facets of games and knowledge)

Coming back to the two components that operate on the grid. I designed several different solutions for how to process DebugTools operations on the grid separately from the state machine (the "rules" of the game).

##### Possible Solutions

First, let's look at the current solution, before introducing DebugTools.

![image](https://user-images.githubusercontent.com/8145874/122866310-976be200-d2dc-11eb-88c7-30c589541ec2.png)

The Mouse component sent any Input (click) and Inquire (hover) events to their respective MessageBrokers. The Grid component directly consumed from these two streams and processed the operations by highlighting hovered-over tiles and selecting clicked-on tiles.

The problem lies in introducing another layer between Mouse and Grid. We introduce StateMachine to forward events to Grid for now (as we have no game rules to apply to the user input). But how do we process DebugTools operations, which will break whatever rules StateMachine defines?

Following are a couple of different solutions I worked through.

-----

![image](https://user-images.githubusercontent.com/8145874/122866651-2c6edb00-d2dd-11eb-8746-800bada9cbf2.png)

Idea: Create a new segment to our full input pipeline. When DebugTools are active, forward events from InputEvents to DebugPipe. Grid consumes from DebugPipe only when DebugTools is active.

Problems: Grid has to "know" about DebugPipe, DebugTools, and when to consume from it vs. InputEvents. I didn't like introducing that dependency to Grid. I want it to only consume from a single stream.

-----

![image](https://user-images.githubusercontent.com/8145874/122866852-7f489280-d2dd-11eb-8254-a71ed2748424.png)

Idea: DebugTools, when active, would disable event reception from InputEvents and make operations directly on the Grid data structures.

Problems: DebugTools has to know about all of the dependencies of any component it wants to override.

-----

![image](https://user-images.githubusercontent.com/8145874/122866955-a7d08c80-d2dd-11eb-84fb-4b075c3356e9.png)

Idea: Create a unique state for DebugTools to run within StateMachine. Whenever DebugTools is active, change to that State and process user input accordingly.

Problems: Conflates game rules with "override" rules i.e. the basic operations that a component can make against Grid. DebugTools now depends on StateMachine instead of treating them both as equal publishers for the event stream.

-----

![image](https://user-images.githubusercontent.com/8145874/122867186-f716bd00-d2dd-11eb-8dd0-d874973d25f0.png)

Idea: DebugTools "pauses" the game rules and substitutes events as its own.

Problems: DebugTools is still seen as a "higher-level" component since now it depends on changing state in StateMachine. No better than a specific game state for DebugTools.

##### Implemented Solution

Finally, I arrived at this solution. This diagram is a bit more detailed, as it was the one I ended up implementing.

![image](https://user-images.githubusercontent.com/8145874/122867376-3f35df80-d2de-11eb-9b23-dafb9d2e8323.png)

The two major design choices that factored into this design was:
* Grid consumes from a single stream and cares not for the publisher
* DebugTools and StateMachine know nothing about one another. They exist on the same level in the control hierarchy.

I was stuck for a while, thinking about what component I would write to "manage" these two components if they were equivalent in their level of control of one another (i.e. no control over one another). I didn't want to write a "DebugToolsManager" or a "StreamManager" or anything else. It felt wrong to add another body-less script (ghost script?) to the God object JUST to manage two other ghost scripts.

I found the parent after sleeping on this puzzle for a couple of nights: the user!

The user is the one that commands all the components to work together, and I can use their "logic" to manage which publisher to this event stream is enabled, and which one is disabled. All I needed to do was to give the user the access to change these boolean "active" flags in the StateMachine and DebugTools components. I did this via the UI component.

The GridEvents stream (labeled GridActions in the diagram), a new segment in our event streaming pipeline, is the main entry point where the Grid actions are published. The Grid will most likely only ever consume from this stream, and nothing else. 

GridActions is a simple enum that specify an operation to make on the Grid store. The intent is that, given a series of valid actions, we can aggregate and compute the state of any grid. This way, bugs in the grid logic can be more easily tracked down, and "down the road" (aka never), we can do a lot with these actions such as:
* save game state
* replay buggy interactions from players' bug reports
* provide starting points for units as a puzzle-like game
* allow players to share their configuration and do SSBU-like replays

None of these will be implemented on this project. But this action/operation scheme is something I've wanted to implement on a tactical game for a while.

I'll now explain how this control scheme is implemented and how I use reactive programming to simplify some of the UI activation code.

#### UI

The DebugMenu UI is a simple Prefab:

<img src=https://user-images.githubusercontent.com/8145874/122868314-a56f3200-d2df-11eb-8942-7a58b53df0e8.png width=300/>

In the UI script (attached to the God component), the [DebugMenu](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-8d19c394398e33e84468840bf673c14f298c2207c18182e5128198fe96d4cfb6R37-R58) is modeled as:
```
public class DebugMenu : MonoBehaviour {

    Dictionary<Options, string> optionLabels = new Dictionary<Options, string>() {
        {Options.ADD_UNIT, "Add Unit"},
        {Options.VIEW_EVENT_QUEUES, "View Event Queues"},
    };

    Dropdown dropDown;
    public ReactiveProperty<Options> selectedOption = new ReactiveProperty<Options>(Options.ADD_UNIT);

    public ReactiveProperty<bool> active = new ReactiveProperty<bool>(false);

    void Start() {
        this.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, -50);

        dropDown = this.GetComponent<Dropdown>();
        dropDown.options = optionLabels.Values.Select(i => new Dropdown.OptionData(i)).ToList();
        dropDown.OnValueChangedAsObservable().Subscribe(e => selectedOption.Value = (Options) e);

        this.GetComponentInChildren<Toggle>().OnValueChangedAsObservable().Subscribe(e => active.Value = e);
    }
}
```

The dropdown's selected value and the toggle's boolean state are exposed with two `ReactiveProperty`s.

The [UI script](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-8d19c394398e33e84468840bf673c14f298c2207c18182e5128198fe96d4cfb6R16-R34) instantiates the DebugMenu prefab and attaches this DebugMenu model to it, before subscribing to the two exposed reactive properties.

```
void Start() {
    this.prefabs = this.GetComponent<Prefabs>();

    this.canvas = GameObject.Instantiate(prefabs.canvasPrefab).GetComponent<Canvas>();

    var debugMenuObj = GameObject.Instantiate(
        prefabs.debugMenuPrefab,
        this.canvas.transform.position,
        Quaternion.identity,
        this.canvas.transform
    );
    debugMenuObj.AddComponent<DebugMenu>();
    debugMenu = debugMenuObj.GetComponent<DebugMenu>();

    debugMenu.active.Subscribe(e => {
        GetComponent<DebugTools>().active.Value = e;
        GetComponent<StateMachine>().active.Value = !e;
    });
}
```

The debugMenu's toggle button now activates either the DebugTools or StateMachine component when toggled on and off. There's a bit of unsophistication here that I don't particularly like. But I will have to expand on this as I work on this project more. I don't think I have enough insight yet to make a good decision here.

#### StateMachine

I've described the StateMachine as the "rules" of the game so far. However, since our game doesn't have any rules at all right now, the StateMachine is an incredibly simple component. 

It subscribes to both Input and Inquire event streams, and when one is received, it immediately forwards it to the GridEvents stream.

Later, this will transmute Input and Inquire event streams into the proper GridEvents. The intent is for the GridEvents stream to only handle GridEvents, and no longer act upon any Input or Inquire events.

#### DebugTools

This... will require some more work. But it did expose a pattern that I'm not completely comfortable with in reactive programming.

Right now, the DebugTools are intended to be a group of inherited classes that provide definitions for `Start` and `CleanUp` methods. This will allow them to hook into any game logic and be easily enabled / disabled. Currently there is a limit of one selected tool at a time, but this can "easily" (famous last words) be modified in the future.

When instantiated, the DebugTools component will subscribe to the debugMenu's exposed value representing the selected tool in the dropdown menu.

```
debugMenu.selectedOption.Subscribe(e => {
    Tool nextTool = toolMap[e];
    if (selectedTool != null && nextTool != selectedTool) {
        selectedTool.Cleanup();
        nextTool.Start();
        selectedTool = nextTool;
    }
});
```

Notice that the DebugTools has to awkwardly keep track of two tools: the currently selected tool, and the tool indicated by the dropdown changed value event. I do wish there was an event stream where you could subscribe and keep state for the last N messages. Then it would be cleaner to operate with changing values instead of keeping that N message-lookback in the component.

The tool itself, [AddUnitTool](https://github.com/zakattak/TacticalGridBase/commit/74c91ff09a97ba662b8e861239c2a6b030126751#diff-152987b11edb9444e6ab972b00b8be5fe89c1b2b60c34f6efca255c9e51f2a91R56-R87), uses two event "transmuters" to parse Input and Inquire events, and turn them into the correct GridEvents for the Grid to operate on.

```
inputToGridTransmuter = datastore.inputEvents.Receive<InputEvent>().Subscribe(e => {
    datastore.gridEvents.Publish(new GridEvent() {
        cell = e.cell,
        publisher = this.GetType().Name,
        action = GridActions.SPAWN_UNIT,
    });
});
hoverToGridTransmuter = datastore.inquireEvents.Receive<HoverEvent>().Subscribe(e => {
    datastore.gridEvents.Publish(new HoverEvent() {
        cell = e.cell,
        publisher = this.GetType().Name,
    });
});
```
The `hoverToGridTransmuter` is forwarding events just like the StateMachine would, which gives me pause.

Since components that operate on events don't know what event publishers they may be overriding... wouldn't that mean every publisher would need to provide an event transmuter for every event stream they are overriding? That seems like it will be much more work, in the long term, and more implicit. My code tools won't provide easy access (or compiling help) if I miss overriding an event that some other downstream consumer depends on. It is concerning, but I'll work to come up with a better solution.

#### Why do any of this
In the last weeks, I've asked myself why the hell I am going through all this trouble to write all this code that doesn't need to be written. It feels like overengineering at best, and a waste of time at worst.

Here's my justification, which I'm not even completely sold on:

I'm not too great at reading code and coming up with a mental map of how it works. I'd like to wager some of the best programmers I know are able to conjure up some sort of diagram in their mind of how it all works together. I don't feel like I have that same natural ability.

Bugs are introduced into code because programmers incorrectly reason about code as they read it, and as they start to patch new logic in. If I can ensure that my code is easier for me to reason about, I'll be less likely to introduce bugs.

I do think that basic, primitive branching and polling loop code for simple games is the way to go. It's certainly faster than all of the infrastructure I've just written. But I know that my own skill level is not capable of handling the mental mapping that happens when you write a near [6000-line controller class](https://github.com/NoelFB/Celeste/blob/master/Source/Player/Player.cs). Maybe one day. But until then, I'll lean a bit more on my architectural scaffolding to help me understand my code a bit more easily, even when I have to come back to it after some time and patch in new features.
