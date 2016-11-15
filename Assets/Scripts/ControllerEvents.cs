using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines and Publishes Vive controller events
/// Add this as a component to each controller for which you'd like to listen to events
/// </summary>
[RequireComponent(typeof(SteamVR_TrackedObject))]
public class ControllerEvents : MonoBehaviour
{

    // Event Declaration Code
    public struct ControllerInteractionEventArgs
    {
        public uint controllerIndex;
    }

    public delegate void ControllerInteractionEventHandler(object sender, ControllerInteractionEventArgs e);

    [System.Serializable]
    public class ControllerEvent : UnityEvent<ControllerInteractionEventArgs> { }

    // Native C# Events are more efficient but can only be used from code
    public event ControllerInteractionEventHandler TriggerPressed;
    public event ControllerInteractionEventHandler TriggerReleased;

    // Unity Events add a little overhead but listeners can be assigned via Unity editor
    public ControllerEvent unityTriggerPressed = new ControllerEvent();
    public ControllerEvent unityTriggerReleased = new ControllerEvent();

    // Member Variables
    [HideInInspector]
    public bool triggerPressed = false;

    private uint controllerIndex;
    private SteamVR_TrackedObject trackedObj;
    private SteamVR_Controller.Device device;

    //Unity lifecycle method
    private void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    //Unity lifecycle method
    private void Update()
    {
        controllerIndex = (uint)trackedObj.index;
        device = SteamVR_Controller.Input((int)controllerIndex);

        //Trigger Pressed
        if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            OnTriggerPressed(SetButtonEvent(ref triggerPressed, true));
        }
        else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
        {
            OnTriggerReleased(SetButtonEvent(ref triggerPressed, false));
        }
    }

    // Creates, fills out, and returns a new ControllerInteractionEventArgs struct
    // (Convenience method to reduce code duplication)
    private ControllerInteractionEventArgs SetButtonEvent(ref bool buttonBool, bool value)
    {
        buttonBool = value;
        ControllerInteractionEventArgs e;
        e.controllerIndex = controllerIndex;
        return e;
    }

    // Event publisher
    public virtual void OnTriggerPressed(ControllerInteractionEventArgs e)
    {
        if (TriggerPressed != null)
        {
            TriggerPressed(this, e);
        }
        unityTriggerPressed.Invoke(e);
    }

    // Event publisher
    public virtual void OnTriggerReleased(ControllerInteractionEventArgs e)
    {
        if (TriggerReleased != null)
        {
            TriggerReleased(this, e);
        }
        unityTriggerReleased.Invoke(e);
    }

}
