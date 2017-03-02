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
        myAnimator = VRTK_DeviceFinder.GetControllerLeftHand().GetComponentInChildren<Animator>();
        GetComponent<VRTK_ControllerEvents>().TriggerAxisChanged += new ControllerInteractionEventHandler(DoTriggerAxisChanged);
    }

    private void DoTriggerAxisChanged(object sender, ControllerInteractionEventArgs e)
    {
        myAnimator.SetFloat("GrabSpeed", e.buttonPressure);
    }
}