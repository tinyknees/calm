using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Add this to any prefab that you want to be able to color.
/// Any models underneath will get additional objects to facilitate coloring.
/// </summary>
public class Colorable : MonoBehaviour {

	// Add UV canvas, camera for render texture, and a brush container to ever model with a mesh renderer
	void Start () {
        Transform[] models = GetComponentsInChildren<Transform>();

        foreach (Transform model in models)
        {
            if ((model.GetComponent<MeshCollider>()) &&
                (model.name != "CanvasBase") &&
                (!model.Find("PaintCanvas")))
            {
                GameObject paintcanvas = new GameObject();
                paintcanvas.name = "PaintCanvas";
                paintcanvas.transform.SetParent(model);
                paintcanvas.transform.localPosition = new Vector3(0, -10, 0);

                GameObject brushcontainer = new GameObject();
                brushcontainer.name = "BrushContainer";
                brushcontainer.transform.SetParent(paintcanvas.transform);
                brushcontainer.transform.localPosition = Vector3.zero;

                GameObject canvascamobj = new GameObject();
                canvascamobj.name = "CanvasCamera";
                canvascamobj.transform.SetParent(paintcanvas.transform);
                canvascamobj.AddComponent<Camera>();
                canvascamobj.transform.localPosition = new Vector3(0, 0, -2);

                Camera canvascamera = canvascamobj.GetComponent<Camera>();
                canvascamera.nearClipPlane = 0.3f;
                canvascamera.farClipPlane = 5;
                canvascamera.clearFlags = CameraClearFlags.Depth;
                canvascamera.enabled = false;
                canvascamera.orthographic = true;
                canvascamera.orthographicSize = 0.5f;

                GameObject canvasbase = new GameObject();
                canvasbase.name = "CanvasBase";
                canvasbase.transform.SetParent(paintcanvas.transform);
                canvasbase.AddComponent<MeshCollider>();
                canvasbase.AddComponent<MeshRenderer>();
                canvasbase.AddComponent<MeshFilter>();
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                canvasbase.GetComponent<MeshFilter>().mesh = quad.GetComponent<MeshFilter>().mesh;
                GameObject.Destroy(quad);
                canvasbase.transform.localPosition = Vector3.zero;

                Material material = new Material(Shader.Find("Unlit/Texture"));
                material.name = "BaseMaterial";
                canvasbase.GetComponent<MeshRenderer>().material = material;
            }
        }
	}

	// Update is called once per frame
	void Update () {

	}
}
