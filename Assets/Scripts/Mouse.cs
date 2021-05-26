using System.Net;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class Mouse : MonoBehaviour {

    IObservable<long> clickStream = Observable.EveryUpdate().Where(_ => Input.GetMouseButtonDown(0));
    List<GameObject> hoverHits = new List<GameObject>();
    Datastore datastore;

    public void Start() {
        datastore = this.GetComponent<Datastore>();
        clickStream.Subscribe(_ => {
            datastore.inputEvents.Publish(InputEvent.mouseDown);
        });
        // clickStream.Subscribe(_ => {
        //     var ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //     hoverHits = Physics2D.RaycastAll(ray, Vector2.zero);
        //     Debug.Log(hoverHits.Count().ToString());
        //     if (hoverHits.Count() > 0) {
        //         datastore.inquireEvents.Publish(
        //             new HoverEvent() { target = hoverHits.First().collider.gameObject }
        //         );
        //     }
        // });

        Observable.EveryUpdate().Where(_ => {
            var ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var tempHits = Physics2D.RaycastAll(ray, Vector2.zero).ToList();
            if (
                tempHits.Count() != hoverHits.Count ||
                tempHits.Any(hit => !hoverHits.Contains(hit.collider.gameObject))
            ) {
                hoverHits = tempHits.Select(hit => hit.collider.gameObject).ToList();
                return true;
            } else {
                return false;
            }
        }).Subscribe(_ => {
            datastore.inquireEvents.Publish(
                new HoverEvent() { targets = hoverHits }
            );
        });
    }
}
