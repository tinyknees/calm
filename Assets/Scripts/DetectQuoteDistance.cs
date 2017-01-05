using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectQuoteDistance : MonoBehaviour {

    public GameObject[] Quotes;
    float Threshold = 2;

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
        int i = 0;
        foreach (GameObject quote in Quotes)
        {
            i++;
            dist = Vector3.Distance(gameObject.transform.position, quote.transform.position);
            //Debug.Log("Distance [" + quote.name + "]: " + dist);

            if (dist < Threshold)
            {

            }
        }

    }
}
