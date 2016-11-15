using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class ScaleUpTree : MonoBehaviour
{
    SteamVR_TrackedObject trackedObj;

    public GameObject[] growObjects; // Setting the type — list of GameObjects

    void Start()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();

        growObjects = GameObject.FindGameObjectsWithTag("Grow"); // Make me a list of every object in my scene with the Grow tag
    }

    void FixedUpdate()
    {
        SteamVR_Controller.Device device = SteamVR_Controller.Input((int)trackedObj.index);
        if (device.GetTouchDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            Debug.Log("You are holding 'Touch Down' on the Trigger");
            foreach (GameObject growObject in growObjects) // Take EACH GameObject in the list “growObjects” and run the code inside the {} brackets
            {
                Debug.Log("Grow!!!");
                Vector3 scale = growObject.transform.localScale;
                scale.x = .1F; // new value
                scale.y = .1F; // new value
                scale.z = .1F; // new value
                growObject.transform.localScale = scale;
            }
        }
    }
}