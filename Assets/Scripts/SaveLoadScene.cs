using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class SaveLoadScene : MonoBehaviour
{

    private Colorable[] colorableObjects;
    private bool loadedTextures;

    // Use this for initialization
    void Start()
    {
        GetComponent<VRTK_ControllerEvents>().AliasMenuOn += new ControllerInteractionEventHandler(HandleMenuPressed);
        GetComponent<VRTK_ControllerEvents>().AliasMenuOff += new ControllerInteractionEventHandler(HandleMenuReleased);

        colorableObjects = FindObjectsOfType<Colorable>();

        Invoke("LoadFile", 4);
    }

    private void Update()
    {
        if (VRTK_DeviceFinder.GetControllerLeftHand(true).activeSelf && !loadedTextures)
        {
            LoadFile();
        }
    }

    // For some reason, the render cameras aren't fast enough to capture
    // the render to texture before the cameras turn off so we are waiting
    // a few secs with all cameras on and turning them off one by one.
    private void LoadFile()
    {
        if (VRTK_DeviceFinder.GetControllerLeftHand(true).activeSelf && !loadedTextures)
        {
            StartCoroutine(LoadTexturesFromFile());
        }
    }


    private void HandleMenuPressed(object sender, ControllerInteractionEventArgs e)
    {
        Debug.Log("pressed");
    }

    private void HandleMenuReleased(object sender, ControllerInteractionEventArgs e)
    {
        Debug.Log("released");
        StartCoroutine(SaveTexturesToFile());
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

    private IEnumerator SaveTexturesToFile()
    {
        string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\SavedCanvas\\";
        if (!System.IO.Directory.Exists(fullPath))
            System.IO.Directory.CreateDirectory(fullPath);
        Debug.Log("Saving to: " + fullPath);

        System.DateTime date = System.DateTime.Now;

        uint i = 0;
        byte[] bytes;
        string filename = "";
        foreach (Colorable co in colorableObjects)
        {
            if (co.saved)
            {
                Texture2D tex = (Texture2D)co.canvasbase.GetComponent<Renderer>().material.mainTexture;
                bytes = tex.EncodeToPNG();
                filename = "CanvasTexture-" + co.name + "-" + i + ".png";
                System.IO.File.WriteAllBytes(fullPath + filename, bytes);
            }
            i++;
            yield return null;
        }

        Debug.Log("<color=orange>Saved Successfully!</color>" + fullPath + filename);
        yield return null;
    }
}
