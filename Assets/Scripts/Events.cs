using UnityEngine;

public class InputEvent {
    public Vector3Int cell;
    public string publisher;
}

public class HoverEvent {
    public Vector3Int cell;
    public string publisher;
}

public class GridEvent {
    public Vector3Int cell;
    public string publisher;
    public GridActions action;
}