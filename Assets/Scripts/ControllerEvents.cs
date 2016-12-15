using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines and Publishes Vive controller events
/// Add this as a component to each controller for which you'd like to listen to events
/// </summary>
[RequireComponent(typeof(SteamVR_TrackedObject))]
public class ControllerEvents : MonoBehaviour
{

    private readonly Vector2 mXAxis = new Vector2(1, 0);
    private readonly Vector2 mYAxis = new Vector2(0, 1);
    private bool trackingSwipe = false;
    private bool checkSwipe = false;

    private Vector2 mStartPosition;
    private Vector2 endPosition;
    private float mSwipeStartTime;

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
    public event ControllerInteractionEventHandler SwipedLeft;
    public event ControllerInteractionEventHandler SwipedRight;
    public event ControllerInteractionEventHandler SwipedUp;
    public event ControllerInteractionEventHandler SwipedDown;
    public event ControllerInteractionEventHandler TouchpadUpPressed;
    public event ControllerInteractionEventHandler TouchpadDownPressed;
    public event ControllerInteractionEventHandler TouchpadRightPressed;
    public event ControllerInteractionEventHandler TouchpadLeftPressed;
    public event ControllerInteractionEventHandler TouchpadReleased;

    // Unity Events add a little overhead but listeners can be assigned via Unity editor
    public ControllerEvent unityTriggerPressed = new ControllerEvent();
    public ControllerEvent unityTriggerReleased = new ControllerEvent();
    public ControllerEvent unitySwipedLeft = new ControllerEvent();
    public ControllerEvent unitySwipedRight = new ControllerEvent();
    public ControllerEvent unitySwipedUp = new ControllerEvent();
    public ControllerEvent unitySwipedDown = new ControllerEvent();
    public ControllerEvent unityTouchpadUpPressed = new ControllerEvent();
    public ControllerEvent unityTouchpadDownPressed = new ControllerEvent();
    public ControllerEvent unityTouchpadRightPressed = new ControllerEvent();
    public ControllerEvent unityTouchpadLeftPressed = new ControllerEvent();
    public ControllerEvent unityTouchpadReleased = new ControllerEvent();

    // Member Variables
    [HideInInspector]
    public bool triggerPressed = false;
    public bool touchpadPressed = false;

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
        else if (device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
        {
            Vector2 touchpad = (device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));

            if (touchpad.y > 0.7f)
            {
                OnTouchpadUpPressed(SetButtonEvent(ref touchpadPressed, true));
            }

            else if (touchpad.y < -0.7f)
            {
                OnTouchpadDownPressed(SetButtonEvent(ref touchpadPressed, true));
            }

            if (touchpad.x > 0.7f)
            {
                OnTouchpadRightPressed(SetButtonEvent(ref touchpadPressed, true));
            }

            else if (touchpad.x < -0.7f)
            {
                OnTouchpadLeftPressed(SetButtonEvent(ref touchpadPressed, true));
            }
        }
        else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
        {
            OnTouchpadReleased(SetButtonEvent(ref touchpadPressed, false));
        }

        if (device.GetTouchDown(Valve.VR.EVRButtonId.k_EButton_Axis0))
        {
            OnTouchpadTouched(SetButtonEvent(ref trackingSwipe, true));
        }
        else if (device.GetTouchUp(Valve.VR.EVRButtonId.k_EButton_Axis0))
        {
            OnTouchpadTouchReleased(SetButtonEvent(ref trackingSwipe, false));
        }

        if (trackingSwipe)
        {
            endPosition = new Vector2(device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).x,
                          device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).y);
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

    // Trigger pull events
    public virtual void OnTriggerPressed(ControllerInteractionEventArgs e)
    {
        if (TriggerPressed != null)
        {
            TriggerPressed(this, e);
        }
        unityTriggerPressed.Invoke(e);
    }

    public virtual void OnTriggerReleased(ControllerInteractionEventArgs e)
    {
        if (TriggerReleased != null)
        {
            TriggerReleased(this, e);
        }
        unityTriggerReleased.Invoke(e);
    }

    // Touchpad Press Down Events
    public virtual void OnTouchpadUpPressed(ControllerInteractionEventArgs e)
    {
        if (TouchpadUpPressed != null)
        {
            TouchpadUpPressed(this, e);
        }
        unityTouchpadUpPressed.Invoke(e);
    }
    public virtual void OnTouchpadDownPressed(ControllerInteractionEventArgs e)
    {
        if (TouchpadDownPressed != null)
        {
            TouchpadDownPressed(this, e);
        }
        unityTouchpadDownPressed.Invoke(e);
    }
    public virtual void OnTouchpadRightPressed(ControllerInteractionEventArgs e)
    {
        if (TouchpadRightPressed != null)
        {
            TouchpadRightPressed(this, e);
        }
        unityTouchpadRightPressed.Invoke(e);
    }
    public virtual void OnTouchpadLeftPressed(ControllerInteractionEventArgs e)
    {
        if (TouchpadLeftPressed != null)
        {
            TouchpadLeftPressed(this, e);
        }
        unityTouchpadLeftPressed.Invoke(e);
    }
    public virtual void OnTouchpadReleased(ControllerInteractionEventArgs e)
    {
        if (TouchpadReleased != null)
        {
            TouchpadReleased(this, e);
        }
        unityTouchpadReleased.Invoke(e);
    }

    public virtual void OnTouchpadTouched(ControllerInteractionEventArgs e)
    {
        mStartPosition = new Vector2(device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).x,
    device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).y);
        mSwipeStartTime = Time.time;
    }

    public virtual void OnTouchpadTouchReleased(ControllerInteractionEventArgs e)
    {
        // The angle range for detecting swipe
        const float mAngleRange = 30;

        // To recognize as swipe user should at lease swipe for this many pixels
        const float mMinSwipeDist = 0.2f;
        float deltaTime = Time.time - mSwipeStartTime;

        // To recognize as a swipe the velocity of the swipe
        // should be at least mMinVelocity
        // Reduce or increase to control the swipe speed
        const float mMinVelocity = 4.0f;

        Vector2 swipeVector = endPosition - mStartPosition;

        float velocity = swipeVector.magnitude / deltaTime;
        if (velocity > mMinVelocity &&
            swipeVector.magnitude > mMinSwipeDist)
        {
            // if the swipe has enough velocity and enough distance


            swipeVector.Normalize();

            float angleOfSwipe = Vector2.Dot(swipeVector, mXAxis);
            angleOfSwipe = Mathf.Acos(angleOfSwipe) * Mathf.Rad2Deg;

            // Detect left and right swipe
            if (angleOfSwipe < mAngleRange)
            {
                if (SwipedRight != null)
                    SwipedRight(this, e);
                unitySwipedRight.Invoke(e);
            }
            else if ((180.0f - angleOfSwipe) < mAngleRange)
            {
                if (SwipedLeft != null)
                    SwipedLeft(this, e);
                unitySwipedLeft.Invoke(e);
            }
            else
            {
                // Detect top and bottom swipe
                angleOfSwipe = Vector2.Dot(swipeVector, mYAxis);
                angleOfSwipe = Mathf.Acos(angleOfSwipe) * Mathf.Rad2Deg;
                if (angleOfSwipe < mAngleRange)
                {
                    if (SwipedUp != null)
                        SwipedUp(this, e);
                    unitySwipedUp.Invoke(e);
                }
                else if ((180.0f - angleOfSwipe) < mAngleRange)
                {
                    if (SwipedDown != null)
                        SwipedDown(this, e);
                    unitySwipedUp.Invoke(e);
                }
            }
        }
    }
}

