using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UniRx;
using System;

public class Datastore : MonoBehaviour {

    public Tilemap activeTilemap;

    public MessageBroker inputEvents = new MessageBroker();
    public MessageBroker inquireEvents = new MessageBroker();

    public void Start() {
        inputEvents.Receive<InputEvent>().Subscribe(e => Debug.Log(e.ToString()));
        inquireEvents.Receive<HoverEvent>().Subscribe(e => Debug.Log($"Hovering over {String.Join(", ", e.targets)}"));
    }
}
