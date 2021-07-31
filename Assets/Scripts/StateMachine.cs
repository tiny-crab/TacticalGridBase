using UnityEngine;
using UniRx;
using System;

public class StateMachine : MonoBehaviour {

    Datastore datastore;

    public BoolReactiveProperty active = new BoolReactiveProperty(false);

    public IDisposable inputTransmuter;
    public IDisposable hoverTransmuter;

    void Start() {
        datastore = this.GetComponent<Datastore>();

        active.Subscribe(e => {
            if (e) {
                StartTransmute();
            } else {
                PauseTransmute();
            }
        });
    }

    void StartTransmute() {
        inputTransmuter = datastore.inputEvents.Receive<InputEvent>()
            .Where(_ => this.active.Value)
            .Subscribe(e => {
                if (datastore.units.ContainsKey(e.cell)) {
                    datastore.gridEvents.Publish(new GridEvent() {
                        cell = e.cell,
                        publisher = this.GetType().Name,
                        action = GridActions.SELECT_UNIT,
                    });
                } else {
                    datastore.gridEvents.Publish(new GridEvent() {
                        cell = e.cell,
                        publisher = this.GetType().Name,
                        action = GridActions.SELECT_TILE,
                    });
                }
            });

        hoverTransmuter = datastore.inquireEvents.Receive<HoverEvent>()
            .Where(_ => this.active.Value)
            .Subscribe(e => {
                datastore.gridEvents.Publish(new HoverEvent() {
                    cell = e.cell,
                    publisher = this.GetType().Name,
                });
            });
    }

    void PauseTransmute() {
        inputTransmuter.Dispose();
        hoverTransmuter.Dispose();
    }

}