using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class Mouse : MonoBehaviour {

    IObservable<long> clickStream = Observable.EveryUpdate().Where(_ => Input.GetMouseButtonDown(0));
    Vector3Int hoveredCoord;
    Datastore datastore;

    public void Start() {
        datastore = this.GetComponent<Datastore>();
        clickStream.Subscribe(_ => {
            datastore.inputEvents.Publish(
                new InputEvent() {
                    cell = GetMouseCellPosition(),
                    publisher = this.GetType().Name,
                }
            );
        });

        Observable.EveryUpdate().Where(_ => {
            if (hoveredCoord != GetMouseCellPosition()) {
                hoveredCoord = GetMouseCellPosition();
                return true;
            } else {
                return false;
            }
        }).Subscribe(_ => {
            datastore.inquireEvents.Publish(
                new HoverEvent() {
                    cell = hoveredCoord,
                    publisher = this.GetType().Name,
                }
            );
        });
    }

    Vector3Int GetMouseCellPosition() {
        var cellPoint = datastore.activeTilemap.WorldToCell(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        return new Vector3Int(cellPoint.x, cellPoint.y, 0);
    }
}
