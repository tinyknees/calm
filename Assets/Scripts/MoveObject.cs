using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// </summary>
[RequireComponent(typeof(ControllerEvents))]
[RequireComponent(typeof(LaserPointer))]
public class MoveObject : MonoBehaviour
{
    private Color laserPointerDefaultColor; // default laser pointer color
    public Camera sceneCamera;  //The camera that looks at the model


    // Flags for state
    private LaserPointer laserPointer; // references the laser coming from controller
    private ControllerEvents controllerEvents; // the controller where event happened
    private LaserPointer.PointerEventArgs hitObj; // shortcut to the object the laser collided with
    private ControllerEvents.ControllerInteractionEventArgs activeController;

    private bool triggerPressed = false; // is the trigger being held
    private bool hitTarget = false; // has the controller laser intersected with an object


    private Vector3 lastControllerPos;
    private bool moved = false;
    private Transform moveTrans;
    private bool pulledDown;
    private Vector3 moveObjOriPos;

    // Unity lifecycle method
    void Awake()
    {
        laserPointer = GetComponent<LaserPointer>();
        controllerEvents = GetComponent<ControllerEvents>();

        laserPointerDefaultColor = Color.clear;
    }

    // Unity lifecycle method
    void OnEnable()
    {
        controllerEvents.TriggerPressed += HandleTriggerPressed;
        controllerEvents.TriggerReleased += HandlerTriggerReleased;
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
    }

    // Unity lifecycle method
    void OnDisable()
    {
        controllerEvents.TriggerPressed -= HandleTriggerPressed;
        controllerEvents.TriggerReleased -= HandlerTriggerReleased;
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
    }

    // Unity lifecycle method
    void Update()
    {
        Movable moveobj = null;

        if (hitObj.target.GetComponent<Movable>() != null)
        {
            moveobj = hitObj.target.GetComponent<Movable>();
        }

        // intersecting with a collider
        if (hitTarget)
        {
            // that is movable
            if (moveobj != null)
            {
                hitObj.target.GetComponent<Renderer>().material.shader = moveobj.outlineShader;
                if (triggerPressed)
                {
                    DoMove();
                    moved = true;
                }
            }
        }

    }

    //Event Handler
    private void HandleTriggerPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        laserPointer.enabled = true;
        triggerPressed = true;
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
        lastControllerPos = controllerEvents.gameObject.transform.position;
    }

    //Event Handler
    private void HandlerTriggerReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        //laserPointer.enabled = false;
        triggerPressed = false;
        if (moved)
        {
            Vector3 moveObjPos = moveTrans.position;
            float sceneCamRef = sceneCamera.transform.position.y - 0.1f;
            // lock it down if pulled past a certain point
            if ((moveObjPos.y < sceneCamRef) && !pulledDown)
            {
                moveObjPos.y = sceneCamRef;
                moveTrans.position = moveObjPos;
                pulledDown = true;
            }
            // not pulled down far enough, return object
            else if ((moveObjPos.y > sceneCamRef) && !pulledDown)
            {
                moveObjPos.y = moveObjOriPos.y;
                moveTrans.position = moveObjPos;
            }
            // object was down. pulling it down to return it.
            else if ((moveObjPos.y < sceneCamRef) && pulledDown)
            {
                moveObjPos.y = moveObjOriPos.y;
                moveTrans.position = moveObjPos;
                pulledDown = false;
            }
            moved = false;
        }
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
    }

    //Event Handler
    private void HandlePointerIn(object sender, LaserPointer.PointerEventArgs e)
    {
        laserPointer.pointerModel.GetComponent<MeshRenderer>().material.color = Color.red;
        hitTarget = true;
        hitObj = e;
    }

    //Event Handler
    private void HandlePointerOut(object sender, LaserPointer.PointerEventArgs e)
    {
        laserPointer.pointerModel.GetComponent<MeshRenderer>().material.color = laserPointerDefaultColor;

        Movable lastmove = e.target.GetComponent<Movable>();
        if (lastmove != null)
        {
            lastmove.GetComponent<Renderer>().material.shader = lastmove.defaultShader;
        }
        hitTarget = false;
    }


    void DoMove ()
    {
        moveTrans = hitObj.target; // set moved object for later reference even when target is out
        if (!moved && !pulledDown)
        {
            moveObjOriPos = moveTrans.position; // set starting position to return to
        }

        Vector3 moveObjPos = moveTrans.position;
        float controllerYMove = controllerEvents.gameObject.transform.position.y - lastControllerPos.y;
        if (controllerYMove < 0)
        {
            moveObjPos.y += controllerYMove * 5f;
            moveTrans.position = moveObjPos;
        }
        lastControllerPos = controllerEvents.gameObject.transform.position;
    }
}
    