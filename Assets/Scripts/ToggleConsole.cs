using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class ToggleConsole : MonoBehaviour {

    private bool touchpadUpPressed = false;
    private bool touchpadReleased = false;

    private GameObject consoleViewer = null;


    private void Start()
    {
        consoleViewer = GetComponentInChildren<VRTK_ConsoleViewer>().gameObject;
        consoleViewer.SetActive(false);
    }

    // Use this for initialization
    private void OnEnable () {
        GetComponent<VRTK_ControllerEvents>().TouchpadPressed += HandleTouchpadPressed;
        GetComponent<VRTK_ControllerEvents>().TouchpadReleased += HandleTouchpadReleased;

    }

    private void OnDisable()
    {
        GetComponent<VRTK_ControllerEvents>().TouchpadPressed -= HandleTouchpadPressed;
        GetComponent<VRTK_ControllerEvents>().TouchpadReleased -= HandleTouchpadReleased;
    }

    // Update is called once per frame
    private void Update () {
        if (touchpadReleased && touchpadUpPressed)
        {
            consoleViewer.SetActive(!consoleViewer.activeSelf);
            touchpadReleased = false;
            touchpadUpPressed = false;
        }
    }

    private void HandleTouchpadPressed(object sender, ControllerInteractionEventArgs e)
    {
        int anglerange = 15;

        if (e.touchpadAngle < 0 + anglerange || e.touchpadAngle > 360 - anglerange)
        {
            touchpadUpPressed = true;
        }
    }

    private void HandleTouchpadReleased(object sender, ControllerInteractionEventArgs e)
    {
        touchpadReleased = true;
    }

}
