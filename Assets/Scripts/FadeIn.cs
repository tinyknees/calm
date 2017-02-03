using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour {

    [Range(0,30)]
    public int fadeTime = 7;
    [Range(0, 30)]
    public int delayTime = 3;

    private Color c;
    private bool faded = false;

    // Use this for initialization
    void Start ()
    {
        c = GetComponent<Text>().color;
    }

    // Update is called once per frame
    void Update ()
    {
        if (!faded)
        {
            faded = true;
            StartCoroutine(fadeIn(delayTime, fadeTime));
        }
    }

    private IEnumerator fadeIn(int delay, int time)
    {
        float t = 0;
        float rate = 1 / (float) delay;
        while (t < 1.0)
        {
            t += Time.deltaTime * rate;
            yield return null;
        }

        t = 0;
        rate = 1 / (float) time;

        while (t < 1.0)
        {
            GetComponent<Text>().color = new Color(c.r, c.g, c.b, Mathf.Lerp(0, 1, t));
            t += Time.deltaTime * rate;
            yield return null;
        }
    }
}
