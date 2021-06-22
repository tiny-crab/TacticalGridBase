using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UniRx;

public class Datastore : MonoBehaviour {
    public Tilemap activeTilemap;
    public Dictionary<Vector3Int, GameObject> units = new Dictionary<Vector3Int, GameObject>();

    public MessageBroker inputEvents = new MessageBroker();
    public MessageBroker inquireEvents = new MessageBroker();

    public MessageBroker gridEvents = new MessageBroker();
}
