using UnityEngine;
using VRTK;

/// <summary>
/// Allows user to move the object by pulling it down like a window blind
/// </summary>
public class MoveObject : VRTK_SimplePointer
{
    private DestinationMarkerEventArgs hitObj;

    private bool triggerPressed = false; // is the trigger being held
    private bool hitTarget = false; // has the controller laser intersected with an object

    private Vector3 lastControllerPos;
    private bool moved = false;
    private Transform moveTrans;
    private bool pulledDown;
    private Vector3 moveObjOriPos;

    protected override void Start()
    {
        base.Start();
        if (GetComponent<VRTK_SimplePointer>() == null)
        {
            Debug.LogError("VRTK_ControllerPointerEvents_ListenerExample is required to be attached to a Controller that has the VRTK_SimplePointer script attached to it");
            return;
        }

        //Setup controller event listeners
        GetComponent<VRTK_ControllerEvents>().TriggerPressed += new ControllerInteractionEventHandler(DoTriggerPressed);
        GetComponent<VRTK_ControllerEvents>().TriggerReleased += new ControllerInteractionEventHandler(DoTriggerReleased);
        GetComponent<MoveObject>().DestinationMarkerEnter += new DestinationMarkerEventHandler(DoPointerIn);
        GetComponent<MoveObject>().DestinationMarkerExit += new DestinationMarkerEventHandler(DoPointerOut);
        GetComponent<MoveObject>().DestinationMarkerSet += new DestinationMarkerEventHandler(DoPointerDestinationSet);

    }
    protected override void AliasRegistration(bool state)
    {
        if (controller)
        {
            if (state)
            {
                controller.TriggerPressed += new ControllerInteractionEventHandler(EnablePointerBeam);
                controller.TriggerReleased += new ControllerInteractionEventHandler(DisablePointerBeam);
                controller.TriggerPressed += new ControllerInteractionEventHandler(SetPointerDestination);
            }
            else
            {
                controller.TriggerPressed -= new ControllerInteractionEventHandler(EnablePointerBeam);
                controller.TriggerReleased -= new ControllerInteractionEventHandler(DisablePointerBeam);
                controller.TriggerPressed -= new ControllerInteractionEventHandler(SetPointerDestination);
            }
        }
    }

    private void DebugLogger(uint index, string action, Transform target, RaycastHit raycastHit, float distance, Vector3 tipPosition)
    {
        string targetName = (target ? target.name : "<NO VALID TARGET>");
        string colliderName = (raycastHit.collider ? raycastHit.collider.name : "<NO VALID COLLIDER>");
        Debug.Log("Controller on index '" + index + "' is " + action + " at a distance of " + distance + " on object named [" + targetName + "] on the collider named [" + colliderName + "] - the pointer tip position is/was: " + tipPosition);
    }

    private void DebugLogger(uint index, string button, string action, ControllerInteractionEventArgs e)
    {
        Debug.Log("Controller on index '" + index + "' " + button + " has been " + action
                + " with a pressure of " + e.buttonPressure + " / trackpad axis at: " + e.touchpadAxis + " (" + e.touchpadAngle + " degrees)");
    }

    private void DoTriggerPressed(object sender, ControllerInteractionEventArgs e)
    {
        triggerPressed = true;
        lastControllerPos = VRTK_DeviceFinder.GetControllerLeftHand(true).transform.position;
        //DebugLogger(e.controllerIndex, "TRIGGER", "pressed", e);
    }

    private void DoTriggerReleased(object sender, ControllerInteractionEventArgs e)
    {
        triggerPressed = false;
        if (hitObj.target != null)
        {
            if (hitObj.target.GetComponent<VRTK_InteractableObject>() != null)
            {
                hitObj.target.GetComponent<VRTK_InteractableObject>().ToggleHighlight(false);
            }
        }
        if (moved)
        {
            Vector3 moveObjPos = moveTrans.position;
            float sceneCamRef = VRTK_DeviceFinder.HeadsetTransform().position.y - 0.1f;
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
    }

    private void DoPointerIn(object sender, DestinationMarkerEventArgs e)
    {
        if (e.target.GetComponent<Movable>() != null)
        {
            hitTarget = true;
            hitObj = e;
            if (e.target.GetComponent<VRTK_InteractableObject>() != null)
            {
                e.target.GetComponent<VRTK_InteractableObject>().ToggleHighlight(true);
            }
        }
    }

    private void DoPointerOut(object sender, DestinationMarkerEventArgs e)
    {
        if (!triggerPressed)
        {
            if (e.target.GetComponent<VRTK_InteractableObject>() != null)
            {
                e.target.GetComponent<VRTK_InteractableObject>().ToggleHighlight(false);
            }
            hitTarget = false;
        }
    }

    private void DoPointerDestinationSet(object sender, DestinationMarkerEventArgs e)
    {
        // DebugLogger(e.controllerIndex, "POINTER DESTINATION", e.target, e.raycastHit, e.distance, e.destinationPosition);
    }

    private void DoMove()
    {
        moveTrans = hitObj.target; // set moved object for later reference even when target is out
        if (!moved && !pulledDown)
        {
            moveObjOriPos = moveTrans.position; // set starting position to return to
        }

        Vector3 moveObjPos = moveTrans.position;
        float controllerYMove = VRTK_DeviceFinder.GetControllerLeftHand(true).transform.position.y - lastControllerPos.y;
        if (controllerYMove < 0)
        {
            moveObjPos.y += controllerYMove * 5f;
            moveTrans.position = moveObjPos;
        }
        lastControllerPos = VRTK_DeviceFinder.GetControllerLeftHand(true).transform.position;
    }


    protected override void Update()
    {
        base.Update();

        // intersecting with a collider
        if (hitTarget)
        {
            if (hitObj.target.GetComponent<Movable>() != null)
            {
                if (triggerPressed)
                {
                    DoMove();
                    moved = true;
                }
            }
        }
    }
}
    