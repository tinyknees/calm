using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this to any prefab that you want to be able to color.
/// Any models underneath will get additional objects to facilitate coloring.
/// </summary>
public class Colorable : MonoBehaviour {

    [HideInInspector]
    public uint index;
    [HideInInspector]
    public GameObject paintcanvas;
    [HideInInspector]
    public GameObject brushcontainer;
    [HideInInspector]
    public GameObject canvascam;
    [HideInInspector]
    public GameObject canvasbase;
    [HideInInspector]
    public bool saved = false;

    private void Awake()
    {
        if ((transform.GetComponent<Collider>()) && (!transform.Find("PaintCanvas")))
        {
            if (paintcanvas == null) { paintcanvas = new GameObject(); }
            paintcanvas.name = "PaintCanvas";
            paintcanvas.transform.SetParent(transform);
            index = VRTK.VRTK_DeviceFinder.GetControllerRightHand().GetComponent<ColorObject>().numCanvas++;
            paintcanvas.transform.localPosition = new Vector3(0, -10 * (index + 1), 0);

            if (brushcontainer == null) { brushcontainer = new GameObject(); }
            brushcontainer.name = "BrushContainer";
            brushcontainer.transform.SetParent(paintcanvas.transform);
            brushcontainer.transform.localPosition = Vector3.zero;

            // look for quote in the object
            Quote quote = GetComponentInChildren<Quote>();
            if (quote != null)
            {
                Vector3 quotepos = quote.transform.localPosition;
                Quaternion quoterot = quote.transform.localRotation;
                quote.transform.SetParent(paintcanvas.transform);
                quotepos.z = -0.01f;
                quoterot.eulerAngles = Vector3.zero + new Vector3(0, 0, quoterot.eulerAngles.z);
                quote.transform.localPosition = quotepos;
                quote.transform.localRotation = quoterot;
            }

            if (canvascam == null) { canvascam = new GameObject(); }
            canvascam.name = "CanvasCamera";
            canvascam.transform.SetParent(paintcanvas.transform);
            canvascam.AddComponent<Camera>();
            canvascam.transform.localPosition = new Vector3(0, 0, -2);

            Camera canvascamera = canvascam.GetComponent<Camera>();
            canvascamera.nearClipPlane = 0.3f;
            canvascamera.farClipPlane = 5;
            canvascamera.clearFlags = CameraClearFlags.Depth;
            canvascamera.enabled = false;
            canvascamera.orthographic = true;
            canvascamera.orthographicSize = 0.5f;

            ResetBase();


            string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\SavedCanvas\\CanvasTexture-" + name + "-" + index + ".png";

            if (System.IO.File.Exists(fullPath))
            {
                System.DateTime date = System.DateTime.Now;
                byte[] bytes;
                string filename = "";

                bytes = System.IO.File.ReadAllBytes(fullPath + filename);
                Texture2D tex = new Texture2D(1024, 1024);
                tex.LoadImage(bytes);
                canvasbase.GetComponent<Renderer>().material.mainTexture = tex;
                canvascam.GetComponent<Camera>().Render();
            }

        }
    }

    /// <summary>
    /// Creates a new base canvas and destroys the old one. Used for initialization and resetting.
    /// </summary>
    /// <param name="co">GameObject of type Colorable</param>
    public void ResetBase()
    {
        if (canvasbase != null)
        {
            Destroy(canvasbase);
        }
        canvasbase = new GameObject();

        canvasbase.name = "CanvasBase";
        canvasbase.transform.SetParent(paintcanvas.transform);
        canvasbase.AddComponent<MeshCollider>();
        canvasbase.AddComponent<MeshRenderer>();
        canvasbase.AddComponent<MeshFilter>();
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        canvasbase.GetComponent<MeshFilter>().mesh = quad.GetComponent<MeshFilter>().mesh;
        GameObject.Destroy(quad);
        canvasbase.transform.localPosition = Vector3.zero;


        Material material = new Material(VRTK.VRTK_DeviceFinder.GetControllerRightHand().GetComponent<ColorObject>().unlitTexture);
        material.name = "BaseMaterial";
        canvasbase.GetComponent<MeshRenderer>().material = material;

        Texture2D coltexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        coltexture.SetPixel(1, 1, VRTK.VRTK_DeviceFinder.GetControllerRightHand().GetComponent<ColorObject>().baseColour);
        coltexture.Apply();
        canvasbase.GetComponent<MeshRenderer>().material.mainTexture = coltexture;

        RenderTexture rt = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32);
        rt.name = "PaintTexture";
        rt.Create();

        Camera canvascamera = canvascam.GetComponent<Camera>();

        canvascamera.targetTexture = rt;

        canvascamera.enabled = true;
        GetComponent<Renderer>().material.mainTexture = rt;
        canvascamera.enabled = false;
    }

}
