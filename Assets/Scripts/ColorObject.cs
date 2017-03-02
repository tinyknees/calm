using UnityEngine;
using System.Collections;
using System;
using VRTK;

/// <summary>
/// </summary>
/// 

public class ColorObject : MonoBehaviour
{
    #region Public Variables
    [Tooltip("Set to Unlit/Texture")]
    public Shader unlitTexture;

    [Header("Colour Pencil Settings", order = 2)]
    public Color[] brushColors = new Color[8];
    public Sprite brushSprite;
    public float brushSize = 0.2f; //The size of our brush

    [Tooltip("Base color of all the colorable objects.")]
    public Color baseColour = Color.black; // Default object color

    [Tooltip("Distance to objects before coloring starts.")]
    [Range(0f, 0.2f)]
    public float brushDistance = 0.05f; // min distance before painting starts

    [HideInInspector]
    public uint numCanvas = 0;
    #endregion

    private Color laserPointerDefaultColor; // default laser pointer color

    // Flags for state
    private LaserPointer laserPointer; // references the laser coming from controller
    private LaserPointer.PointerEventArgs hitObj; // shortcut to the object the laser collided with
    private LaserPointer.PointerEventArgs invHitObj; // shortcut to the object the eraser collided with

    private bool hitTarget = false; // has the controller laser intersected with an object
    private bool invHitTarget = false;

    private Material baseMaterial; // The material of our base texture (Where we will save the painted texture)

    private Color brushColor; //The selected color
    private int brushCounter = 0, MAX_BRUSH_COUNT = 1000; //To avoid having millions of brushes
    private Transform pieRing;
    private bool saving = false; //Flag to check if we are saving the texture

    private Transform savObj;
    private GameObject brushContainer; // Our container for the brushes painted
    private Camera canvasCam; // The camera that looks at the canvas
    private RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes
    private bool loadedTextures;

    int colorIndex = 0;

    private Colorable[] colorableObjects;

    private bool camerasOff = false;

    // swipe variables
    private event ControllerInteractionEventHandler SwipedRight;
    private event ControllerInteractionEventHandler SwipedLeft;
    private readonly Vector2 touchXAxis = new Vector2(1, 0);
    private readonly Vector2 touchYAxis = new Vector2(0, 1);
    private float swipeStartTime;
    private Vector2 swipeStart;
    private Vector2 swipeEnd;

    #region Initialization

    void Init()
    {
        // go through all colorable objects and create canvases for them
        colorableObjects = FindObjectsOfType<Colorable>();

        int j = 0;
        pieRing = transform.Find("Pencil").Find("Pie");
        if (pieRing != null)
        {
            foreach (Transform pie in pieRing)
            {
                pie.GetComponent<Renderer>().material.color = brushColors[j];
                j++;
            }
        }

    }

    void Awake()
    {
        laserPointer = GetComponent<LaserPointer>();
        
        laserPointerDefaultColor = Color.clear;

        ChangeBrushColor(colorIndex);

        Init();
    }


    // Subscribe to event handlers
    void OnEnable()
    {
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
        laserPointer.InvPointerIn += HandleInvPointerIn;
        laserPointer.InvPointerOut += HandleInvPointerOut;

        GetComponent<VRTK_ControllerEvents>().TouchpadTouchStart += HandleTouchpadTouchStart;
        GetComponent<VRTK_ControllerEvents>().TouchpadTouchEnd += HandleTouchpadTouchEnd;
        GetComponent<VRTK_ControllerEvents>().TouchpadPressed += HandleTouchpadPressed;
        GetComponent<VRTK_ControllerEvents>().TouchpadAxisChanged += HandleTouchpadAxisChanged;
    }


    // Unsubscribe from event handlers
    void OnDisable()
    {
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
        laserPointer.InvPointerIn -= HandleInvPointerIn;
        laserPointer.InvPointerOut -= HandleInvPointerOut;

        GetComponent<VRTK_ControllerEvents>().TouchpadTouchStart -= HandleTouchpadTouchStart;
        GetComponent<VRTK_ControllerEvents>().TouchpadTouchEnd -= HandleTouchpadTouchEnd;
        GetComponent<VRTK_ControllerEvents>().TouchpadPressed -= HandleTouchpadPressed;
        GetComponent<VRTK_ControllerEvents>().TouchpadAxisChanged -= HandleTouchpadAxisChanged;
    }

    #endregion

    void Update()
    {

        if (hitTarget && hitObj.distance < brushDistance)
        {
            MenuSelect ms = hitObj.target.GetComponent<MenuSelect>();
            if (hitObj.target.GetComponent<Colorable>() != null)
            {
                DoColor(brushColor, hitObj);
            }
            else if (ms != null)
            {
                ms.Activate();
            }
        }
        else if (invHitTarget && invHitObj.distance < brushDistance + 0.145)
        {
            DoColor(baseColour, invHitObj);
        }
        else
        {
            // not coloring, stop sounds if any coloring sound is playing
            AudioSource audio = transform.GetComponent<AudioSource>();
            if (audio.isPlaying)
            {
                audio.Stop();
            }
        }

    }



    #region Event Handlers

    private void HandlePointerIn(object sender, LaserPointer.PointerEventArgs e)
    {
        hitTarget = true;
        hitObj = e;
        laserPointer.PointerUpdate += HandlePointerUpdate;
    }

    private void HandlePointerOut(object sender, LaserPointer.PointerEventArgs e)
    {
        laserPointer.pointerModel.GetComponent<MeshRenderer>().material.color = laserPointerDefaultColor;
        hitTarget = false;
        laserPointer.PointerUpdate -= HandlePointerUpdate;

        if (!saving)
        {
            saving = true;
            Invoke("SaveTexture", 0.1f);
        }

        
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

    private void HandleTouchpadPressed(object sender, ControllerInteractionEventArgs e)
    {
        int anglerange = 10;

        if (e.touchpadAngle < 270 + anglerange && e.touchpadAngle > 270 - anglerange)
        {
            ChangeBrushColor(colorIndex - 1);
        }
        else if(e.touchpadAngle < 90 + anglerange && e.touchpadAngle > 90 - anglerange)
        {
            ChangeBrushColor(colorIndex + 1);
        }
    }

    public void HandleTouchpadTouchStart(object sender, ControllerInteractionEventArgs e)
    {
        swipeStart = new Vector2(e.touchpadAxis.x, e.touchpadAxis.y);
        swipeStartTime = Time.time;
    }

    public void HandleTouchpadAxisChanged(object sender, ControllerInteractionEventArgs e)
    {
        swipeEnd = new Vector2(e.touchpadAxis.x, e.touchpadAxis.y);
    }

    private void HandleTouchpadTouchEnd(object sender, ControllerInteractionEventArgs e)
    {
        Debug.Log("HandleTouchpadTouchEnd");
        // The angle range for detecting swipe
        const float anglerange = 30;

        // To recognize as swipe user should at lease swipe for this many pixels
        const float minswipedist = 0.2f;
        float deltaTime = Time.time - swipeStartTime;

        // To recognize as a swipe the velocity of the swipe
        // Reduce or increase to control the swipe speed
        const float minvel = 4.0f;

        Vector2 swipeVector = swipeEnd - swipeStart;

        float velocity = swipeVector.magnitude / deltaTime;
        if (velocity > minvel &&
            swipeVector.magnitude > minswipedist)
        {
            // if the swipe has enough velocity and enough distance


            swipeVector.Normalize();

            float angleOfSwipe = Vector2.Dot(swipeVector, touchXAxis);
            angleOfSwipe = Mathf.Acos(angleOfSwipe) * Mathf.Rad2Deg;

            // Detect left and right swipe
            if (angleOfSwipe < anglerange)
            {
                ChangeBrushColor(colorIndex - 1);
            }
            else if ((180.0f - angleOfSwipe) < anglerange)
            {
                ChangeBrushColor(colorIndex + 1);
            }
        }
    }


    #endregion

    #region Colouring Functions

    void ChangeBrushColor(int c)
    {
        if (c == brushColors.Length)
        {
            colorIndex = 0;
        } else if (c < 0)
        {
            colorIndex = brushColors.Length - 1;
        } else
        {
            colorIndex = c;
        }
        if (pieRing)
        {
            pieRing.localRotation = Quaternion.Euler(colorIndex * 360 / brushColors.Length, 0, 0);
        }
        brushColor = brushColors[colorIndex];
        gameObject.GetComponentInChildren<Renderer>().material.color = brushColors[colorIndex];
    }


    /// <summary>
    /// Colours the hit target supplied
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

            AudioSource audio = GetComponent<AudioSource>();
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

    #endregion

    /// <summary>
    /// Checks the texture to see if there's a quote and if the quote's been revealed
    /// </summary>
    /// <param name="tex">The render texture to check against</param>
    /// <returns></returns>
    private IEnumerator CheckQuote(Texture2D tex)
    {
        if (savObj != null)
        {
            // check if there is a quote to check against
            Quote quote = savObj.GetComponentInChildren<Quote>();
            if (quote != null)
            {
                Transform canvasbase = savObj.GetComponent<Colorable>().canvasbase.transform;

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

                        // Test which pixels are no longer the base color i.e., revealed
                        if (colors[index] != baseColour)
                        {
                            //Debug.Log(quote.name + ": " +
                            //    colors[index].r.ToString("F6") + "," +
                            //    colors[index].g.ToString("F6") + "," +
                            //    colors[index].b.ToString("F6") + " " +
                            //    colors[index].a.ToString("F6") + " " +
                            //    baseColor.r.ToString("F6") + "," +
                            //    baseColor.g.ToString("F6") + "," +
                            //    baseColor.b.ToString("F6") + "," +
                            //    baseColor.a.ToString("F6")
                            //    );
                            //yield return null;
                            colored++;
                        }
                    }
                }

                float percentrevealed = colored / (quotesize.x / basesize.x * quotesize.y / basesize.y * colors.Length) * 100;

                // Debug.Log(quote.name + ": " + percentrevealed + "% revealed.");

                // Start playing sounds and enable recording if sufficient amount revealed
                quote.revealed = (percentrevealed > quote.revealThreshold) ? true : false;
            }
        }

        yield return null;
    }

    /// <summary>
    /// Compares two colours to check if they're approximately the same colour
    /// </summary>
    /// <param name="ac">a color</param>
    /// <param name="bc">b color</param>
    /// <returns>true if same, false if not</returns>
    private bool CompareColors(Color ac, Color bc)
    {
        bool r = false;
        bool g = false;
        bool b = false;
        int ct = 1; // color threshold

        if (Math.Round(ac.r) <= Math.Round(bc.r) + ct && Math.Round(ac.r) > Math.Round(bc.r - ct))
            r = true;
        if (Math.Round(ac.g) <= Math.Round(bc.g) + ct && Math.Round(ac.g) > Math.Round(bc.g - ct))
            g = true;
        if (Math.Round(ac.b) <= Math.Round(bc.b) + ct && Math.Round(ac.b) > Math.Round(bc.b - ct))
            b = true;

        return (r && g && b) ? true : false;
    }

    private void PickCanvas (Transform target)
    {
        if (target != null)
        {
            if (target != savObj)
            {
                savObj = target;
                canvasCam = target.GetComponentInChildren<Camera>();
                brushContainer = savObj.GetComponent<Colorable>().brushcontainer;
            }

            if (!canvasCam.enabled) { canvasCam.enabled = true; }
        }
    }

    #region Loading and Saving
    //Sets the base material with a our canvas texture, then removes all our brushes
    void SaveTexture()
    {
        if (brushCounter > 0)
        {
            System.DateTime date = System.DateTime.Now;
            brushCounter = 0;

            PickCanvas(savObj);

            foreach (Colorable co in colorableObjects)
            {
                if (co.name == savObj.name)
                {
                    co.saved = true;
                }
            }

            Quote quote = savObj.GetComponentInChildren<Quote>();
            if (quote != null)
            {
                quote.gameObject.SetActive(false);
            }

            RenderTexture canvas = canvasCam.targetTexture;
            RenderTexture.active = canvas;
            Texture2D tex = new Texture2D(canvas.width, canvas.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, canvas.width, canvas.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            baseMaterial = savObj.GetComponent<Colorable>().canvasbase.transform.GetComponent<MeshRenderer>().material;
            baseMaterial.mainTexture = tex; //Put the painted texture as the base

            foreach (Transform child in brushContainer.transform)
            {//Clear brushes
                Destroy(child.gameObject);
            }
            canvasCam.enabled = false;

            if (quote != null)
            {
                quote.gameObject.SetActive(true);
            }

            StartCoroutine("CheckQuote", tex);

            StartCoroutine(SaveTexturesToFile());
        }

        saving = false;
    }
    

    private IEnumerator SaveTexturesToFile()
    {
        string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\SavedCanvas\\";
        if (!System.IO.Directory.Exists(fullPath))
            System.IO.Directory.CreateDirectory(fullPath);
        Debug.Log("Saving to: " + fullPath);

        DateTime date = DateTime.Now;

        uint i = 0;
        byte[] bytes;
        string filename = "";
        foreach (Colorable co in colorableObjects)
        {
            if (co.saved)
            {
                Texture2D tex = (Texture2D)co.canvasbase.GetComponent<Renderer>().material.mainTexture;
                bytes = tex.EncodeToPNG();
                filename = "CanvasTexture-" + co.name + "-" + co.index + ".png";
                System.IO.File.WriteAllBytes(fullPath + filename, bytes);
            }
            i++;
            yield return null;
        }

        Debug.Log("<color=orange>Saved Successfully!</color>" + fullPath + filename);
        yield return null;
    }

    #endregion
}

