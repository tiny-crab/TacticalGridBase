using UnityEngine;
using System;

public class Unit : MonoBehaviour {

    public int moveRange;

    public void Start () {
        moveRange = new System.Random().Next(1,5);
    }

}