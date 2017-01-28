using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quote : MonoBehaviour {

    [HideInInspector]
    public bool revealed = false;
    [HideInInspector]
    public bool recorded = false;
    [HideInInspector]
    public bool pinged = false;
    [Tooltip("Amount of a quote that needs to be revealed before triggering.")]
    [Range(0,100)]
    public int revealThreshold = 75;

}
