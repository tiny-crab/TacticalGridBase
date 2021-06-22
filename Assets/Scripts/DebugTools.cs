using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class DebugTools : MonoBehaviour {

    Datastore datastore;
    DebugMenu debugMenu;

    public BoolReactiveProperty active = new BoolReactiveProperty(false);
    public Tool selectedTool;

    public enum Options {
        ADD_UNIT,
        VIEW_EVENT_QUEUES,
    }

    public static Dictionary<Options, Tool> toolMap;

    void Start() {
        datastore = GetComponent<Datastore>();
        debugMenu = GetComponent<UI>().debugMenu;

        toolMap = new Dictionary<Options, Tool>() {
            {Options.ADD_UNIT, new AddUnitTool(datastore)},
            {Options.VIEW_EVENT_QUEUES, new AddUnitTool(datastore)},
        };

        selectedTool = toolMap[debugMenu.selectedOption.Value];
        selectedTool.Start();

        debugMenu.selectedOption.Subscribe(e => {
            Tool nextTool = toolMap[e];
            if (selectedTool != null && nextTool != selectedTool) {
                selectedTool.Cleanup();
                nextTool.Start();
                selectedTool = nextTool;
            }
        });

        active.Subscribe(e => {
            if (e) {
                selectedTool.Start();
            } else {
                selectedTool.Cleanup();
            }
        });
    }

    public abstract class Tool {
        public virtual void Start() {}
        public virtual void Cleanup() {}
    }

    public class AddUnitTool : Tool {

        IDisposable inputToGridTransmuter;
        IDisposable hoverToGridTransmuter;

        Datastore datastore;

        public AddUnitTool(Datastore datastore) {
            this.datastore = datastore;
        }

        public override void Start() {
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
        }

        public override void Cleanup() {
            inputToGridTransmuter.Dispose();
            hoverToGridTransmuter.Dispose();
        }
    }
}