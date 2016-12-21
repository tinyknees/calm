using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ControllerEvents))]

public class Record : MonoBehaviour {
    public GameObject audioContainer;
    private ControllerEvents controllerEvents; // the controller where event happened
    private ControllerEvents.ControllerInteractionEventArgs activeController;
    private AudioSource audiosource;
    private bool startRecording = true;
    private bool startPlaying = true;

    private bool touchpadUpPressed = false;
    private bool touchpadDownPressed = false;

    void Awake()
    {
        if (audioContainer == null)
        {
            audioContainer = new GameObject();
            audioContainer.name = "Recording Container";
        }
        if (audioContainer.GetComponent<AudioSource>() == null)
        {
            audioContainer.AddComponent<AudioSource>();
        }
        audiosource = audioContainer.GetComponent<AudioSource>();

        controllerEvents = GetComponent<ControllerEvents>();
    }

    void OnEnable()
    {
        controllerEvents.TouchpadUpPressed += HandleTouchpadUpPressed;
        controllerEvents.TouchpadDownPressed += HandleTouchpadDownPressed;
        controllerEvents.TouchpadReleased += HandleTouchpadReleased;
    }
    void OnDisable()
    {
        controllerEvents.TouchpadUpPressed -= HandleTouchpadUpPressed;
        controllerEvents.TouchpadDownPressed -= HandleTouchpadDownPressed;
        controllerEvents.TouchpadReleased -= HandleTouchpadReleased;
    }

    // Update is called once per frame
    void Update ()
    {
        if (touchpadDownPressed)
        {
            if (startRecording)
            {
                if (!Microphone.IsRecording(null))
                {
                    Debug.Log("Started Recording");
                    audiosource.clip = Microphone.Start(null, true, 60, 44100);
                }
            }
            else
            {
                Debug.Log("Stopped Recording");
                Microphone.End(null);
            }
        }
        else if (touchpadUpPressed)
        {
            if (startPlaying)
            {
                if (!audiosource.isPlaying)
                {
                    Debug.Log("Started Playing");
                    audiosource.Play();
                }
            }
            else
            {
                Debug.Log("Stopped Playing");
                audiosource.Stop();
            }
        }
    }

    private void HandleTouchpadUpPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadUpPressed = true;
        startPlaying = startPlaying ? false : true;
    }

    private void HandleTouchpadDownPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadDownPressed = true;
        startRecording = startRecording ? false : true;
    }

    private void HandleTouchpadReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadDownPressed = false;
        touchpadUpPressed = false;
    }

}