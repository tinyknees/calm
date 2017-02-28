using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class Menu : MonoBehaviour
{

    private bool loadedTextures = false;
    private Colorable[] colorableObjects;
    

    // Use this for initialization
    void Start()
    {
        GetComponent<VRTK_ControllerEvents>().AliasMenuOn += new ControllerInteractionEventHandler(HandleMenuPressed);
        GetComponent<VRTK_ControllerEvents>().AliasMenuOff += new ControllerInteractionEventHandler(HandleMenuReleased);

        colorableObjects = FindObjectsOfType<Colorable>();
    }

    private void HandleMenuPressed(object sender, ControllerInteractionEventArgs e)
    {
        Debug.Log("pressed");
    }

    private void HandleMenuReleased(object sender, ControllerInteractionEventArgs e)
    {
        Debug.Log("released");
        StartCoroutine(ResetScene());
    }

    private IEnumerator ResetScene()
    {
        string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\SavedCanvas\\";
        if (System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.Delete(fullPath, true);
        }

        foreach (Colorable co in colorableObjects)
        {
            Debug.Log(co.name);
            VRTK.VRTK_DeviceFinder.GetControllerRightHand(true).GetComponent<ColorObject>().CreateCanvasBase(co);
            yield return null;
        }
        foreach (Colorable co in colorableObjects)
        {
            co.canvascam.GetComponent<Camera>().Render();
            yield return null;
        }
    }

    private IEnumerator LoadTexturesFromFile()
    {
        string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\SavedCanvas\\";
        if (System.IO.Directory.Exists(fullPath))
        {
            System.DateTime date = System.DateTime.Now;
            uint i = 0;
            byte[] bytes;
            string filename = "";
            foreach (Colorable co in colorableObjects)
            {
                filename = "CanvasTexture-" + co.name + "-" + i + ".png";
                i++;
                if (System.IO.File.Exists(fullPath + filename))
                {
                    bytes = System.IO.File.ReadAllBytes(fullPath + filename);
                    Texture2D tex = new Texture2D(1024, 1024);
                    tex.LoadImage(bytes);
                    co.canvasbase.GetComponent<Renderer>().material.mainTexture = tex;
                    co.canvascam.GetComponent<Camera>().Render();
                }
            }
            yield return null;
        }

        loadedTextures = true;

    }

}
