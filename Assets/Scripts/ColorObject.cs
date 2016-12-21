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
    private LaserPointer.PointerEventArgs invHitObj; // shortcut to the object the eraser collided with
    private ControllerEvents.ControllerInteractionEventArgs activeController;

    private bool triggerPressed = false; // is the trigger being held
    private bool hitTarget = false; // has the controller laser intersected with an object
    private bool invHitTarget = false;


    // Painting specific globals
    public GameObject brushCursor; //The cursor that overlaps the model
    public Sprite cursorPaint; // Cursor for the differen functions 
    public Color defaultColor = Color.black; // Default object color
    private Material baseMaterial; // The material of our base texture (Where we will save the painted texture)

    public float brushSize = 0.2f; //The size of our brush
    public float brushDistance = 0.05f; // min distance before painting starts
    public bool cursorActive = false;

    private Color brushColor; //The selected color
    private int brushCounter = 0, MAX_BRUSH_COUNT = 1000; //To avoid having millions of brushes
    private bool saving = false; //Flag to check if we are saving the texture

    private Transform savObj;
    private GameObject brushContainer; // Our container for the brushes painted
    private Camera canvasCam; // The camera that looks at the canvas
    private RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes

    int colorIndex = 0;

    private Colorable[] colorableObjects;

    void Init()
    {
        // go through all colorable objects and create canvases for them
        colorableObjects = FindObjectsOfType<Colorable>();

        uint i = 0;

        foreach (Colorable co in colorableObjects)
        {
            if ((co.transform.GetComponent<Collider>()) &&
                (!co.transform.Find("PaintCanvas")))
            {
                if (co.paintcanvas == null) { co.paintcanvas = new GameObject(); }
                co.paintcanvas.name = "PaintCanvas";
                co.paintcanvas.transform.SetParent(co.transform);
                co.paintcanvas.transform.localPosition = new Vector3(0, -10 * (i+1), 0);

                if (co.brushcontainer == null) { co.brushcontainer = new GameObject(); }
                co.brushcontainer.name = "BrushContainer";
                co.brushcontainer.transform.SetParent(co.paintcanvas.transform);
                co.brushcontainer.transform.localPosition = Vector3.zero;

                if (co.canvascam == null) { co.canvascam = new GameObject(); }
                co.canvascam.name = "CanvasCamera";
                co.canvascam.transform.SetParent(co.paintcanvas.transform);
                co.canvascam.AddComponent<Camera>();
                co.canvascam.transform.localPosition = new Vector3(0, 0, -2);

                Camera canvascamera = co.canvascam.GetComponent<Camera>();
                canvascamera.nearClipPlane = 0.3f;
                canvascamera.farClipPlane = 5;
                canvascamera.clearFlags = CameraClearFlags.Depth;
                canvascamera.enabled = false;
                canvascamera.orthographic = true;
                canvascamera.orthographicSize = 0.5f;

                if (co.canvasbase == null) { co.canvasbase = new GameObject(); }
                co.canvasbase.name = "CanvasBase";
                co.canvasbase.transform.SetParent(co.paintcanvas.transform);
                co.canvasbase.AddComponent<MeshCollider>();
                co.canvasbase.AddComponent<MeshRenderer>();
                co.canvasbase.AddComponent<MeshFilter>();
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                co.canvasbase.GetComponent<MeshFilter>().mesh = quad.GetComponent<MeshFilter>().mesh;
                GameObject.Destroy(quad);
                co.canvasbase.transform.localPosition = Vector3.zero;

                Material material = new Material(Shader.Find("Unlit/Texture"));
                material.name = "BaseMaterial";
                co.canvasbase.GetComponent<MeshRenderer>().material = material;

                RenderTexture rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32);
                rt.name = "PaintTexture";
                rt.Create();
                co.canvascam.GetComponent<Camera>().targetTexture = rt;

                GameObject brushObj = (GameObject)Instantiate(Resources.Load("BrushEntity"));
                brushObj.GetComponent<SpriteRenderer>().color = defaultColor;
                brushObj.transform.parent = co.brushcontainer.transform;
                brushObj.transform.localPosition = Vector3.zero;
                brushObj.transform.localScale = Vector3.one;

                RenderTexture canvas = co.canvascam.GetComponent<Camera>().targetTexture;
                RenderTexture.active = canvas;
                Texture2D tex = new Texture2D(canvas.width, canvas.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, canvas.width, canvas.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                baseMaterial = co.canvasbase.transform.GetComponent<MeshRenderer>().material;
                baseMaterial.mainTexture = tex; //Put the painted texture as the base

                co.canvascam.GetComponent<Camera>().enabled = true;
                co.GetComponent<MeshRenderer>().material.mainTexture = rt;

            }
            i++;
        }
    }

    // Unity lifecycle method
    void Awake()
    {
        laserPointer = GetComponent<LaserPointer>();
        controllerEvents = GetComponent<ControllerEvents>();
        
        laserPointerDefaultColor = Color.clear;

        brushCursor.GetComponent<SpriteRenderer>().sprite = cursorPaint;
        brushColor = brushColors[colorIndex];

        brushCursor.transform.localScale *= brushSize;
        ChangeBrushColor();

        Init();
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
        if (hitTarget && hitObj.distance < brushDistance)
        {
            DoColor(brushColor, hitObj);
            UpdateBrushCursor();
        }
        else if (invHitTarget && invHitObj.distance < brushDistance + 0.145)
        {
            DoColor(defaultColor, invHitObj);
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
        brushCursor.SetActive(false);
        saving = true;
        Invoke("SaveTexture", 0.1f);
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
        } else if (colorIndex < 0)
        {
            colorIndex = brushColors.Length - 1;
        }
        brushColor = brushColors[colorIndex];
        controllerEvents.gameObject.GetComponentInChildren<Renderer>().material.color = brushColors[colorIndex];
        brushCursor.GetComponent<SpriteRenderer>().material.color = brushColors[colorIndex];
    }


    //To update at realtime the painting cursor on the mesh, off by default but good for debugging
    void UpdateBrushCursor()
    {
        if (cursorActive)
        {
            Vector3 uvWorldPosition = Vector3.zero;
            if (HitTestUVPosition(ref uvWorldPosition, hitObj) && !saving)
            {
                brushCursor.SetActive(true);
                brushCursor.transform.position = uvWorldPosition + brushContainer.transform.position;
            }
            else
            {
                brushCursor.SetActive(false);
            }
        }
    }


    void DoColor(Color bcolor, LaserPointer.PointerEventArgs hit)
    {
        // While saving brush strokes onto the texture, don't allow coloring
        if (saving)
            return;

        Vector3 uvWorldPosition = Vector3.zero;

        if (HitTestUVPosition(ref uvWorldPosition, hit))
        {
            GameObject brushObj;

            brushObj = (GameObject)Instantiate(Resources.Load("BrushEntity")); //Paint a brush

            //TODO: Replace this with public properties and assignable brushes
            if (hit.angle < 130)
            {
                brushSize = 0.25f;
                bcolor.a = brushSize * 2.0f; // Brushes have alpha to have a merging effect when painted over.
            }
            else if (hit.angle < 140)
            {
                brushSize = 0.2f;
                bcolor.a = brushSize * 2.0f; // Brushes have alpha to have a merging effect when painted over.
            }
            else if (hit.angle < 150)
            {
                brushSize = 0.10f;
                bcolor.a = 0.6f; // Brushes have alpha to have a merging effect when painted over.
            }
            else
            {
                brushSize = 0.05f;
                bcolor.a = 1f; // Brushes have alpha to have a merging effect when painted over.
            }
            brushObj.GetComponent<SpriteRenderer>().color = bcolor; //Set the brush color

            AudioSource audio = controllerEvents.transform.GetComponent<AudioSource>();
            if (!audio.isPlaying)
            {
                audio.Play();
            }

            SteamVR_Controller.Input((int)hit.controllerIndex).TriggerHapticPulse(100);

            brushObj.transform.parent = brushContainer.transform; //Add the brushstroke to our container to be wiped later
            brushObj.transform.localPosition = uvWorldPosition; //The position of the brush (in the UVMap)
            brushObj.transform.localScale = Vector3.one * brushSize;//The size of the brush

            brushCounter++; //Add to the max brushes

            //If we reach the max brushes available, flatten the texture and clear the brushes
            if (brushCounter >= MAX_BRUSH_COUNT)
            {
                brushCursor.SetActive(false);
                saving = true;
                Invoke("SaveTexture", 0.1f);
            }
        }
    }

    //Returns the position on the texuremap according to a hit in the mesh collider
    bool HitTestUVPosition(ref Vector3 uvWorldPosition, LaserPointer.PointerEventArgs hit)
    {
        // check that a target was hit and that it's marked as something we can color
        if ((hit.target != null) && (hit.target.GetComponent<Colorable>() != null))
        {
            PickCanvas(hit.target);

            MeshCollider meshCollider = hit.target.GetComponent<Collider>() as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return false;
            Vector2 pixelUV = new Vector2(hit.textureCoord.x, hit.textureCoord.y);
            uvWorldPosition.x = pixelUV.x - canvasCam.orthographicSize;//To center the UV on X
            uvWorldPosition.y = pixelUV.y - canvasCam.orthographicSize;//To center the UV on Y
            uvWorldPosition.z = 0.0f;

            return true;
        }
        else
        {
            return false;
        }

    }

    //Sets the base material with a our canvas texture, then removes all our brushes
    void SaveTexture()
    {
       if (brushCounter > 0)
       {
            System.DateTime date = System.DateTime.Now;
            brushCounter = 0;

            PickCanvas(savObj);

            RenderTexture canvas = canvasCam.targetTexture;
            RenderTexture.active = canvas;
            Texture2D tex = new Texture2D(canvas.width, canvas.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, canvas.width, canvas.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            baseMaterial = savObj.FindChild("PaintCanvas").FindChild("CanvasBase").transform.GetComponent<MeshRenderer>().material;
            baseMaterial.mainTexture = tex; //Put the painted texture as the base
            foreach (Transform child in brushContainer.transform)
            {//Clear brushes
                Destroy(child.gameObject);
            }
        }
        ////StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
        Invoke("ShowCursor", 0.1f);
    }

    void PickCanvas (Transform target)
    {
        if (target != null)
        {
            if (target != savObj)
            {
                canvasCam = target.GetComponentInChildren<Camera>();
                brushContainer = target.Find("PaintCanvas").Find("BrushContainer").gameObject;
                savObj = target;
            }

            //// set up textures if none already
            //if (canvasCam.targetTexture == null)
            //{
            //    RenderTexture rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32);
            //    rt.name = "PaintTexture";
            //    rt.Create();
            //    canvasCam.targetTexture = rt;

            //    GameObject brushObj = (GameObject)Instantiate(Resources.Load("BrushEntity"));
            //    brushObj.GetComponent<SpriteRenderer>().color = brushColor;
            //    brushObj.transform.parent = brushContainer.transform;
            //    brushObj.transform.localPosition = Vector3.zero;
            //    brushObj.transform.localScale = Vector3.one * 3;
            //    RenderTexture canvas = canvasCam.targetTexture;
            //    RenderTexture.active = canvas;
            //    Texture2D tex = new Texture2D(canvas.width, canvas.height, TextureFormat.RGB24, false);
            //    tex.ReadPixels(new Rect(0, 0, canvas.width, canvas.height), 0, 0);
            //    tex.Apply();
            //    RenderTexture.active = null;
            //    baseMaterial = savObj.FindChild("PaintCanvas").FindChild("CanvasBase").transform.GetComponent<MeshRenderer>().material;
            //    baseMaterial.mainTexture = tex; //Put the painted texture as the base


            //    canvasCam.enabled = true;
            //    target.GetComponent<MeshRenderer>().material.mainTexture = rt;
            //}

        }
    }


    //Show again the user cursor (To avoid saving it to the texture)
    void ShowCursor()
    {
        saving = false;
        brushCursor.SetActive(true);
    }

    ////////////////// PUBLIC METHODS //////////////////
    public void SetBrushSize(float newBrushSize)
    { //Sets the size of the cursor brush or decal
        brushSize = newBrushSize;
        brushCursor.transform.localScale = Vector3.one * brushSize;
    }

    ////////////////// OPTIONAL METHODS //////////////////

#if !UNITY_WEBPLAYER
    IEnumerator SaveTextureToFile(Texture2D savedTexture)
    {
        brushCounter = 0;
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

