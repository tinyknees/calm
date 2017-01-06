using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectQuoteDistance : MonoBehaviour {

    public GameObject[] Quotes;
    [Range(0, 3f)]
    [Tooltip("Distance to objects before coloring starts.")]
    public float distanceThreshold = 2;

	// Use this for initialization
	void Start () {
        // check if quotes are assigned
		if (Quotes.Length > 0)
        {
            foreach (GameObject quote in Quotes)
            {
                if (quote == null)
                {
                    Debug.LogError("Quotes need to be assigned.");
                }
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
        float dist;
        float nearestDist = 100f;
        GameObject nearestQuote = null;

        int i = 0;

        foreach (GameObject quote in Quotes)
        {
            if (quote != null)
            {
                i++;

                dist = Vector3.Distance(gameObject.transform.position, quote.transform.position);

                if (nearestQuote == null || nearestDist > dist)
                {
                    nearestDist = dist;
                    nearestQuote = quote;
                }

                //                Debug.Log("Distance [" + quote.name + "]: " + dist);
                //Debug.Log("nearest: " + nearestQuote.name);
                if (dist < distanceThreshold)
                {

                }
            }
        }

    }
}
