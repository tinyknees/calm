using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// </summary>
[RequireComponent(typeof(ControllerEvents))]
[RequireComponent(typeof(LaserPointer))]
public class ColorObject : MonoBehaviour
{

    public Color[] brushColors = new Color[5];
    private Color laserPointerDefaultColor; // default laser pointer color

    // Flags for state
    private LaserPointer laserPointer; // references the laser coming from controller
    private ControllerEvents controllerEvents; // the controller where event happened
    private LaserPointer.PointerEventArgs hitObj; // shortcut to the object the laser collided with
    private ControllerEvents.ControllerInteractionEventArgs activeController;

    private bool triggerPressed = false; // is the trigger being held
    private bool hitTarget = false; // has the controller laser intersected with an object

    // Painting specific globals
    public Sprite cursorPaint; // Cursor for the differen functions 

    public float brushSize = 0.2f; //The size of our brush
    public float brushDistance = 0.05f; // min distance before painting starts

    private Color brushColor; //The selected color

    int colorIndex = 0;
    private LaserPointer.PointerEventArgs invHitObj;
    private bool invHitTarget;

    // Unity lifecycle method
    void Awake()
    {
        laserPointer = GetComponent<LaserPointer>();
        controllerEvents = GetComponent<ControllerEvents>();
        
        laserPointerDefaultColor = Color.clear;
        brushColor = brushColors[colorIndex];
        ChangeBrushColor();
    }

    // Unity lifecycle method
    void OnEnable()
    {
        controllerEvents.TriggerPressed += HandleTriggerPressed;
        controllerEvents.TriggerReleased += HandlerTriggerReleased;
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
        laserPointer.InvPointerIn += HandleInvPointerIn;
        laserPointer.InvPointerOut += HandleInvPointerOut;
        controllerEvents.SwipedRight += HandleSwipedRight;
        controllerEvents.SwipedLeft += HandleSwipedLeft;
    }

    // Unity lifecycle method
    void OnDisable()
    {
        controllerEvents.TriggerPressed -= HandleTriggerPressed;
        controllerEvents.TriggerReleased -= HandlerTriggerReleased;
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
        laserPointer.InvPointerIn -= HandleInvPointerIn;
        laserPointer.InvPointerOut -= HandleInvPointerOut;
        controllerEvents.SwipedRight -= HandleSwipedRight;
        controllerEvents.SwipedLeft -= HandleSwipedLeft;
    }

    // Unity lifecycle method
    void Update()
    {
        if (hitTarget &&
            hitObj.distance < brushDistance &&
            hitObj.target.GetComponent<Colorable>() != null)
        {
            DoColor(brushColor, hitObj);
        }
        else if (invHitTarget &&
                (invHitObj.distance < brushDistance + 0.145) &&
                 invHitObj.target.GetComponent<Colorable>() != null)
        {
            DoColor(Color.white, invHitObj);
        }
        else
        {
            // not coloring, stop sounds if any coloring sound is playing
            AudioSource audio = controllerEvents.transform.GetComponent<AudioSource>();
            if (audio.isPlaying)
            {
                audio.Stop();
            }
        }
    }

    //Event Handler
    private void HandleTriggerPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        triggerPressed = true;
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
        laserPointer.InvPointerIn -= HandleInvPointerIn;
        laserPointer.InvPointerOut -= HandleInvPointerOut;
        activeController = e;
    }

    //Event Handler
    private void HandlerTriggerReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        triggerPressed = false;
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
        laserPointer.InvPointerIn += HandleInvPointerIn;
        laserPointer.InvPointerOut += HandleInvPointerOut;
        activeController = e;
    }

    //Event Handler
    private void HandlePointerIn(object sender, LaserPointer.PointerEventArgs e)
    {
        //laserPointer.pointerModel.GetComponent<MeshRenderer>().enabled = false;
        hitTarget = true;
        hitObj = e;
        laserPointer.PointerUpdate += HandlePointerUpdate;
    }

    //Event Handler
    private void HandlePointerOut(object sender, LaserPointer.PointerEventArgs e)
    {
        laserPointer.pointerModel.GetComponent<MeshRenderer>().material.color = laserPointerDefaultColor;
        hitTarget = false;
        laserPointer.PointerUpdate -= HandlePointerUpdate;
    }

    private void HandlePointerUpdate(object sender, LaserPointer.PointerEventArgs e)
    {
        hitObj = e;
    }

    private void HandleInvPointerIn(object sender, LaserPointer.PointerEventArgs e)
    {
        invHitTarget = true;
        invHitObj = e;
        laserPointer.InvPointerUpdate += HandleInvPointerUpdate;
    }
    private void HandleInvPointerOut(object sender, LaserPointer.PointerEventArgs e)
    {
        invHitTarget = false;
        laserPointer.InvPointerUpdate -= HandleInvPointerUpdate;
    }

    private void HandleInvPointerUpdate(object sender, LaserPointer.PointerEventArgs e)
    {
        invHitObj = e;
    }


    private void HandleSwipedLeft(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        colorIndex--;
        ChangeBrushColor();
        activeController = e;
    }

    private void HandleSwipedRight(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        colorIndex++;
        ChangeBrushColor();
        activeController = e;
    }

    void ChangeBrushColor()
    {
        if (colorIndex == brushColors.Length)
        {
            colorIndex = 0;
        }
        else if (colorIndex < 0)
        {
            colorIndex = brushColors.Length - 1;
        }
        brushColor = brushColors[colorIndex];
        controllerEvents.gameObject.GetComponentInChildren<Renderer>().material.color = brushColors[colorIndex];
    }

    void DoColor(Color bcolor, LaserPointer.PointerEventArgs hit)
    {
        AudioSource audio = controllerEvents.transform.GetComponent<AudioSource>();
        if (!audio.isPlaying)
        {
            audio.Play();
        }

        SteamVR_Controller.Input((int)hit.controllerIndex).TriggerHapticPulse(100);

        //TODO: Replace this with public properties and assignable brushes
        if (hit.angle < 130)
        {
            brushSize = 0.005f;
            brushColor.a = 0.2f; // Brushes have alpha to have a merging effect when painted over.
        }
        else if (hit.angle < 140)
        {
            brushSize = 0.003f;
            brushColor.a = 0.2f; // Brushes have alpha to have a merging effect when painted over.
        }
        else if (hit.angle < 150)
        {
            brushSize = 0.0025f;
            brushColor.a = 0.8f; // Brushes have alpha to have a merging effect when painted over.
        }
        else
        {
            brushSize = 0.001f;
            brushColor.a = 0.9f; // Brushes have alpha to have a merging effect when painted over.
        }

        Debug.Log(hitObj.target.name);
        RenderTexture PaintTarget = hitObj.target.GetComponent<Colorable>().PaintTarget;
        Material PaintShader = hitObj.target.GetComponent<Colorable>().PaintShader;
        RenderTexture TempRenderTarget;
        TempRenderTarget = new RenderTexture(PaintTarget.width, PaintTarget.height, 0);
        PaintShader.SetColor("PaintBrushColour", bcolor);
        PaintShader.SetFloat("PaintBrushSize", brushSize);
        PaintShader.SetVector("PaintUv", hit.textureCoord);
        Graphics.Blit(PaintTarget, TempRenderTarget);
        Graphics.Blit(TempRenderTarget, PaintTarget, PaintShader);
    }

    ////////////////// OPTIONAL METHODS //////////////////

#if !UNITY_WEBPLAYER
    IEnumerator SaveTextureToFile(Texture2D savedTexture)
    {
        string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\UserCanvas\\";
        System.DateTime date = System.DateTime.Now;
        string fileName = "CanvasTexture.png";
        if (!System.IO.Directory.Exists(fullPath))
            System.IO.Directory.CreateDirectory(fullPath);
        var bytes = savedTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath + fileName, bytes);
        Debug.Log("<color=orange>Saved Successfully!</color>" + fullPath + fileName);
        yield return null;
    }
#endif
}

