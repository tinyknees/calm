using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSelect : MonoBehaviour {

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject);
        if (other.name == "Pencil")
        {
            GetComponent<Renderer>().material.color = Color.red;
            MenuManager mm = VRTK.VRTK_DeviceFinder.GetControllerLeftHand(true).GetComponentInChildren<MenuManager>();

            Debug.Log("Selected: " + name.Substring(5));

            switch (name.Substring(5))
            {
                case ("0"):
                    StartCoroutine(mm.Screenshot());
                    break;
                case ("1"):
                    break;
                case ("2"):
                    StartCoroutine(mm.ResetScene());
                    break;
                default:
                    break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "Pencil")
        {
            GetComponent<Renderer>().material.color = Color.white;
        }
    }
    public void Activate()
    {
    }

}
