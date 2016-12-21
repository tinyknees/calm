using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

    void Awake()
    {
        paintcanvas = new GameObject();
        brushcontainer = new GameObject();
        canvascam = new GameObject();
        canvasbase = new GameObject();
    }
}
