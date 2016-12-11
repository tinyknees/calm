using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]

/// <summary>
/// Add this to any prefab that you want to be able to color.
/// Any models underneath will get additional objects to facilitate coloring.
/// </summary>
public class Colorable : MonoBehaviour {

    public Color ClearColour;
    public Material PaintShader;
    public RenderTexture PaintTarget;
    private RenderTexture TempRenderTarget;
    private Material ThisMaterial;

    void Start()
    {
        if (ThisMaterial == null)
            ThisMaterial = this.GetComponent<Renderer>().material;

        //	already setup
        if (PaintTarget != null)
            if (ThisMaterial.mainTexture == PaintTarget)
                return;

        //	copy texture
        if (ThisMaterial.mainTexture != null)
        {
            if (PaintTarget == null)
                PaintTarget = new RenderTexture(ThisMaterial.mainTexture.width, ThisMaterial.mainTexture.height, 0);
            Graphics.Blit(ThisMaterial.mainTexture, PaintTarget);
            ThisMaterial.mainTexture = PaintTarget;
        }
        else
        {
            if (PaintTarget == null)
                PaintTarget = new RenderTexture(1024, 1024, 0);

            //	clear if no existing texture
            Texture2D ClearTexture = new Texture2D(1, 1);
            ClearTexture.SetPixel(0, 0, ClearColour);
            Graphics.Blit(ClearTexture, PaintTarget);
            ThisMaterial.mainTexture = PaintTarget;

        }
        if (PaintShader == null)
        {
            PaintShader = (Material)Instantiate(Resources.Load("PaintTexture"));
        }
    }
}
