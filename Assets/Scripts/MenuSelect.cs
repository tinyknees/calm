using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSelect : MonoBehaviour {

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name == "[CameraRig]")
        {
            GetComponent<Renderer>().material.color = Color.red;
            MenuManager mm = collision.gameObject.GetComponentInChildren<MenuManager>();
            Debug.Log(mm.gameObject.name);
            Debug.Log("Selected: " + name);
            StartCoroutine(mm.ResetScene());

        }
    }

    void OnCollisionExit(Collision collision)
    {
        GetComponent<Renderer>().material.color = Color.white;
    }
}
