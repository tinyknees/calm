using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSkybox : MonoBehaviour {

    public float RotationPerSecond = 0.5f;
    public bool Rotate = true;

    protected void Update()
    {
        if (Rotate) RenderSettings.skybox.SetFloat("_Rotation", Time.time * RotationPerSecond);
    }
}
