using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this to any prefab that you want to be able to color.
/// Any models underneath will get additional objects to facilitate coloring.
/// </summary>
public class Colorable : MonoBehaviour {

    [HideInInspector]
    public GameObject paintcanvas;
    [HideInInspector]
    public GameObject brushcontainer;
    [HideInInspector]
    public GameObject canvascam;
    [HideInInspector]
    public GameObject canvasbase;
    [HideInInspector]
    public bool saved = false;
}
