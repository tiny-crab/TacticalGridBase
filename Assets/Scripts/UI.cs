using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Unity.Linq;
using System.Collections.Generic;
using Options = DebugTools.Options;

public class UI : MonoBehaviour {

    Prefabs prefabs;

    Canvas canvas;
    public DebugMenu debugMenu;

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
}

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