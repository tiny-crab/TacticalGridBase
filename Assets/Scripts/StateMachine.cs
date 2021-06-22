using UnityEngine;
using UniRx;

public class StateMachine : MonoBehaviour {

    Datastore datastore;

    public BoolReactiveProperty active = new BoolReactiveProperty(false);

    void Start() {
        datastore = this.GetComponent<Datastore>();

        // automatically forward any events received to grid
        // eventually, these events will be transmuted due to the state of the game
        datastore.inputEvents.Receive<InputEvent>().Subscribe(e => {
            if (active.Value) {
                datastore.gridEvents.Publish(new InputEvent() {
                    cell = e.cell,
                    publisher = this.GetType().Name,
                });
            }
        });
        datastore.inquireEvents.Receive<HoverEvent>().Subscribe(e => {
            if (active.Value) {
                datastore.gridEvents.Publish(new HoverEvent() {
                    cell = e.cell,
                    publisher = this.GetType().Name,
                });
            }
        });
    }

}