using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class HandAnimator : MonoBehaviour
{

    private Animator myAnimator;

    // Use this for initialization
    void Start()
    {
        myAnimator = VRTK_DeviceFinder.GetControllerLeftHand(true).GetComponentInChildren<Animator>();
        GetComponent<VRTK_ControllerEvents>().TriggerTouchStart += new ControllerInteractionEventHandler(DoTriggerTouchStart);
        GetComponent<VRTK_ControllerEvents>().TriggerTouchEnd += new ControllerInteractionEventHandler(DoTriggerTouchEnd);
    }

    private void DoTriggerTouchStart(object sender, ControllerInteractionEventArgs e)
    {
        myAnimator.SetBool("Grab", true);
    }

    private void DoTriggerTouchEnd(object sender, ControllerInteractionEventArgs e)
    {
        myAnimator.SetBool("Grab", false);
    }
}