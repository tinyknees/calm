using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// </summary>
[RequireComponent(typeof(ControllerEvents))]
[RequireComponent(typeof(LaserPointer))]
public class PaintObject : MonoBehaviour
{
    private Color laserPointerDefaultColor; // default laser pointer color

    // Flags for state
    private LaserPointer laserPointer; // references the laser coming from controller
    private ControllerEvents controllerEvents; // the controller where event happened
    private LaserPointer.PointerEventArgs hitObj; // shortcut to the object the laser collided with
    private ControllerEvents.ControllerInteractionEventArgs activeController;

    private bool triggerPressed = false; // is the trigger being held
    private bool hitTarget = false; // has the controller laser intersected with an object

    // Painting specific globals
    public GameObject brushCursor; //The cursor that overlaps the model
    public Camera sceneCamera;  //The camera that looks at the model
    public Sprite cursorPaint; // Cursor for the differen functions 
    public Material baseMaterial; // The material of our base texture (Where we will save the painted texture)

    private float brushSize = 0.5f; //The size of our brush
    private Color brushColor; //The selected color
    private int brushCounter = 0, MAX_BRUSH_COUNT = 1000; //To avoid having millions of brushes
    private bool saving = false; //Flag to check if we are saving the texture

    private GameObject brushContainer; // Our container for the brushes painted
    private Camera canvasCam; // The camera that looks at the canvas
    private RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes

    private Color[] brushColors = new Color[3];
    int colorIndex = 0;
    private const float BRUSH_DISTANCE = 0.15f;


    // Unity lifecycle method
    void Awake()
    {
        laserPointer = GetComponent<LaserPointer>();
        controllerEvents = GetComponent<ControllerEvents>();

        laserPointerDefaultColor = Color.clear;

        brushCursor.GetComponent<SpriteRenderer>().sprite = cursorPaint;
        brushColors[0] = Color.red;
        brushColors[1] = Color.blue;
        brushColors[2] = Color.yellow;
        brushColor = brushColors[0];

        brushCursor.transform.localScale *= brushSize;
    }

    // Unity lifecycle method
    void OnEnable()
    {
        controllerEvents.TriggerPressed += HandleTriggerPressed;
        controllerEvents.TriggerReleased += HandlerTriggerReleased;
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
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
    }

    // Unity lifecycle method
    void Update()
    {
        if (hitTarget && hitObj.distance < BRUSH_DISTANCE)
        {
            DoPaint();
            //UpdateBrushCursor();
        }
    }

    //Event Handler
    private void HandleTriggerPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        triggerPressed = true;
        laserPointer.PointerIn -= HandlePointerIn;
        laserPointer.PointerOut -= HandlePointerOut;
    }

    //Event Handler
    private void HandlerTriggerReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        triggerPressed = false;
        laserPointer.PointerIn += HandlePointerIn;
        laserPointer.PointerOut += HandlePointerOut;
    }

    //Event Handler
    private void HandlePointerIn(object sender, LaserPointer.PointerEventArgs e)
    {
        laserPointer.pointerModel.GetComponent<MeshRenderer>().material.color = Color.clear;
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

    private void HandleSwipedLeft(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        colorIndex--;
        ChangeBrushColor();
    }

    private void HandleSwipedRight(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        colorIndex++;
        ChangeBrushColor();
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
        //brushCursor.GetComponent<SpriteRenderer>().material.color = brushColors[colorIndex];
    }

    void DoPaint()
    {
        if (saving)
            return;
        Vector3 uvWorldPosition = Vector3.zero;
        if (HitTestUVPosition(ref uvWorldPosition))
        {
            GameObject brushObj;

            brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a brush
            brushObj.GetComponent<SpriteRenderer>().color = brushColor; //Set the brush color

            brushColor.a = brushSize * 2.0f; // Brushes have alpha to have a merging effect when painted over.
            brushObj.transform.parent = brushContainer.transform; //Add the brush to our container to be wiped later
            brushObj.transform.localPosition = uvWorldPosition; //The position of the brush (in the UVMap)
            brushObj.transform.localScale = Vector3.one * brushSize;//The size of the brush
        }
        brushCounter++; //Add to the max brushes
        if (brushCounter >= MAX_BRUSH_COUNT)
        { //If we reach the max brushes available, flatten the texture and clear the brushes
            brushCursor.SetActive(false);
            saving = true;
            Invoke("SaveTexture", 0.1f);

        }
    }


    //To update at realtime the painting cursor on the mesh
    void UpdateBrushCursor()
    {
        Vector3 uvWorldPosition = Vector3.zero;
        if (HitTestUVPosition(ref uvWorldPosition) && !saving)
        {
            brushCursor.SetActive(true);
            brushCursor.transform.position = uvWorldPosition + brushContainer.transform.position;
        }
        else
        {
            brushCursor.SetActive(false);
        }
    }


    //Returns the position on the texuremap according to a hit in the mesh collider
    bool HitTestUVPosition(ref Vector3 uvWorldPosition)
    {
        if (hitObj.target != null)
        {
            // use appropriate camera and brush container for the object we are looking at
            canvasCam = hitObj.target.FindChild("PaintCanvas").GetComponentInChildren<Camera>();
            brushContainer = hitObj.target.transform.FindChild("PaintCanvas").FindChild("BrushContainer").gameObject;

            // set up textures if none already
            if (canvasCam.targetTexture == null)
            {
                RenderTexture rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32);
                rt.name = "PaintTexture";
                rt.Create();
                canvasCam.targetTexture = rt;
                canvasCam.enabled = true;
                hitObj.target.GetComponent<MeshRenderer>().material.mainTexture = rt;
            }

            MeshCollider meshCollider = hitObj.target.GetComponent<Collider>() as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return false;
            Vector2 pixelUV = new Vector2(hitObj.textureCoord.x, hitObj.textureCoord.y);
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
        //brushCounter = 0;
        //System.DateTime date = System.DateTime.Now;
        //RenderTexture.active = canvasTexture;
        //Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGB24, false);
        //tex.ReadPixels(new Rect(0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
        //tex.Apply();
        //RenderTexture.active = null;
        //baseMaterial.mainTexture = tex; //Put the painted texture as the base
        //foreach (Transform child in brushContainer.transform)
        //{//Clear brushes
        //    Destroy(child.gameObject);
        //}
        ////StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
        Invoke("ShowCursor", 0.1f);
    }


    //Show again the user cursor (To avoid saving it to the texture)
    void ShowCursor()
    {
        saving = false;
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

