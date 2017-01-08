using UnityEngine;
using System.Collections;
using System;
using System.Linq;

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

    private bool hitTarget = false; // has the controller laser intersected with an object
    private bool invHitTarget = false;

    private GameObject[] allQuoteObjects;

    // Painting specific globals
    public GameObject brushCursor; //The cursor that overlaps the model
    [Tooltip("Base color of all the colorable objects.")]
    public Color baseColor = Color.black; // Default object color
    private Material baseMaterial; // The material of our base texture (Where we will save the painted texture)


    public float brushSize = 0.2f; //The size of our brush

    [Tooltip("Distance to objects before coloring starts.")]
    [Range(0f, 0.2f)]
    public float brushDistance = 0.05f; // min distance before painting starts

    [Tooltip("Amount of a quote that needs to be revealed before triggering.")]
    [Range(0f, 100f)]
    public float quotePercentage = 80;

    public bool cursorActive = false;
    
    public Sprite cursorPaint; // Cursor for the different functions 

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

                // look for quote in the object
                Quote quote = co.GetComponentInChildren<Quote>();
                if (quote != null)
                {
                    Vector3 quotepos = quote.transform.localPosition;
                    quote.transform.SetParent(co.paintcanvas.transform);
                    quotepos.z = -0.01f;
                    quote.transform.localPosition = quotepos;
                    quote.transform.localRotation = Quaternion.identity;
                }

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

                Texture2D coltexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                coltexture.SetPixel(1, 1, baseColor);
                coltexture.Apply();
                co.canvasbase.GetComponent<MeshRenderer>().material.mainTexture = coltexture;

                RenderTexture rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32);
                rt.name = "PaintTexture";
                rt.Create();
                co.canvascam.GetComponent<Camera>().targetTexture = rt;

                co.canvascam.GetComponent<Camera>().enabled = true;

                co.GetComponent<MeshRenderer>().material.mainTexture = rt;

            }
            i++;
        }

        allQuoteObjects = GameObject.FindGameObjectsWithTag("Quote");
    }

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

        Invoke("TurnOff", 3);
    }


    // For some reason, the render cameras aren't fast enough to capture
    // the render to texture before the cameras turn off so we are waiting
    // a few secs with all cameras on and turning them off one by one.
    private void TurnOff ()
    {
        StartCoroutine("TurnOffCameras");
    }
    private IEnumerator TurnOffCameras()
    {
        // go through all colorable objects and create canvases for them
        colorableObjects = FindObjectsOfType<Colorable>();

        foreach (Colorable co in colorableObjects)
        {
            co.GetComponentInChildren<Camera>().enabled = false;
            yield return null;
        }
    }

    // Subscribe to event handlers
    void OnEnable()
    {
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
        laserPointer.InvPointerIn += HandleInvPointerIn;
        laserPointer.InvPointerOut += HandleInvPointerOut;
        controllerEvents.SwipedRight += HandleSwipedRight;
        controllerEvents.SwipedLeft += HandleSwipedLeft;
    }

    // Unsubscribe from event handlers
    void OnDisable()
    {
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
        laserPointer.InvPointerIn -= HandleInvPointerIn;
        laserPointer.InvPointerOut -= HandleInvPointerOut;
        controllerEvents.SwipedRight -= HandleSwipedRight;
        controllerEvents.SwipedLeft -= HandleSwipedLeft;
    }


    void Update()
    {
        if (hitTarget && hitObj.distance < brushDistance)
        {
            DoColor(brushColor, hitObj);
            UpdateBrushCursor();
        }
        else if (invHitTarget && invHitObj.distance < brushDistance + 0.145)
        {
            DoColor(baseColor, invHitObj);
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



    /* BUTTON EVENT HANDLERS ----------------------------------------------------------------------*/

    private void HandlePointerIn(object sender, LaserPointer.PointerEventArgs e)
    {
        //laserPointer.pointerModel.GetComponent<MeshRenderer>().enabled = false;
        hitTarget = true;
        hitObj = e;
        laserPointer.PointerUpdate += HandlePointerUpdate;
    }

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


    /* COLORING FUNCTIONS ----------------------------------------------------------------------*/

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

    //Show again the user cursor (To avoid saving it to the texture)
    void ShowCursor()
    {
        saving = false;
        if (cursorActive)
        {
            brushCursor.SetActive(true);
        }
    }

    void SetBrushSize(float newBrushSize)
    { //Sets the size of the cursor brush or decal
        brushSize = newBrushSize;
        brushCursor.transform.localScale = Vector3.one * brushSize;
    }

    /// <summary>
    /// Colors the hit target supplied
    /// </summary>
    /// <param name="bcolor">What colour to colour with.</param>
    /// <param name="hit">A raycast target from laser pointer.</param>
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

            StartCoroutine("CheckQuote", tex);

            foreach (Transform child in brushContainer.transform)
            {//Clear brushes
                Destroy(child.gameObject);
            }
            canvasCam.enabled = false;
        }
        ////StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
        Invoke("ShowCursor", 0.1f);
    }

    private IEnumerator CheckQuote(Texture2D tex)
    {
        if (savObj != null)
        {
            // check if there is a quote to check against
            Quote quote = savObj.GetComponentInChildren<Quote>();
            if (quote != null)
            {
                Transform canvasbase = savObj.FindChild("PaintCanvas").FindChild("CanvasBase").transform;

                Vector2 basesize = canvasbase.GetComponent<Renderer>().bounds.size;
                Vector2 quotesize = quote.gameObject.GetComponent<Renderer>().bounds.size;
                Vector2 baseorigin = new Vector2(canvasbase.position.x - basesize.x / 2, canvasbase.position.y - basesize.y / 2);
                Vector2 quoteorigin = new Vector2(quote.transform.localPosition.x + baseorigin.x + basesize.x / 2 - quotesize.x / 2, quote.transform.localPosition.y + baseorigin.y + basesize.y / 2 - quotesize.y / 2);
                Vector2 quoteend = new Vector2(quoteorigin.x + quotesize.x, quoteorigin.y + quotesize.y);

                //Debug.Log("quotex: " + quoteorigin.x +
                //    " \nquotey: " + quoteorigin.y +
                //    " \nquotex2: " + quoteend.x +
                //    " \nquotey2: " + quoteend.y +
                //    " \nbasex: " + baseorigin.x +
                //    " \nbasey: " + baseorigin.y +
                //    " \nbasesizex: " + basesize.x +
                //    " \nbasesizey: " + basesize.y +
                //    " \nquotesizex: " + quotesize.x +
                //    " \nquotesizey: " + quotesize.y
                //    );

                Color32[] colors = tex.GetPixels32();
                float texsize = (float)Math.Sqrt(colors.Length);

                float ax = (quoteorigin.x - baseorigin.x) / basesize.x * texsize;
                float ay = (quoteorigin.y - baseorigin.y) / basesize.y * texsize;
                float bx = (quoteend.x - baseorigin.x) / basesize.x * texsize;
                float by = (quoteend.y - baseorigin.y) / basesize.y * texsize;

                int index = 0; int x = 0; int y = 0;
                int colored = 0;

                //Debug.Log("ax: " + Math.Round(ax) + " x: " + x + " bx: " + Math.Round(bx) +
                //    " \nay: " + Math.Round(ay) + " y: " + y + " by: " + Math.Round(by));

                for (x = (int)Math.Round(ax); x < Math.Round(bx) - 1; x++)
                {
                    for (y = (int)Math.Round(ay); y < Math.Round(by) - 1; y++)
                    {
                        index = y * (int)texsize + x;

                        // test which pixels are no longer the base color i.e., revealed
                        if (colors[index] != baseColor)
                        {
                            colored++;
                        }
                    }
                }

                float percentrevealed = colored / (quotesize.x / basesize.x * quotesize.y / basesize.y * colors.Length) * 100;
                Debug.Log(quote.name + ": " + percentrevealed + "% revealed.");

                // start playing sounds and enable recording if sufficient amount revealed
                quote.revealed = (percentrevealed > quotePercentage) ? true : false;
            }
        }

        yield return null;
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

            if (!canvasCam.enabled) { canvasCam.enabled = true; }
        }
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

