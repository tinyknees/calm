using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class MenuManager : MonoBehaviour
{
    #region Public Variables
    [Range (0,5)]
    public float menuDelay = 1.0f;
    public GameObject menuObject;

    #endregion

    private bool loadedTextures = false;
    private Colorable[] colorableObjects;
    private float t = 0;
    private bool menuDisplaying = false;
    private float angleThreshold = 25;
    private bool resetting = false;
    GameObject[] menuItems = new GameObject[3];

    // Use this for initialization
    void Start()
    {
        colorableObjects = FindObjectsOfType<Colorable>();
    }

    private void Update()
    {
        Vector3 rot = VRTK_DeviceFinder.GetModelAliasController(gameObject).transform.rotation.eulerAngles;
        if ((Mathf.Abs(rot.x) > 360 - angleThreshold ||
             Mathf.Abs(rot.x) < angleThreshold) &&
            (Mathf.Abs(rot.z) > 180 - angleThreshold &&
             Mathf.Abs(rot.z) < 180 + angleThreshold))
        {
            t += Time.deltaTime;
        }
        else
        {
            t = 0;
            if (menuDisplaying)
            {
                Debug.Log("hiding menu");
                StopCoroutine(DisplayMenu());
                StartCoroutine(HideMenu());
            }
        }

        if (t > menuDelay)
        {
            if (!menuDisplaying) { StartCoroutine(DisplayMenu()); }
        }
    } 

    public IEnumerator ResetScene()
    {
        if (!resetting)
        {
            Debug.Log("resetting");
            resetting = true;
            string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\SavedCanvas\\";
            if (System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.Delete(fullPath, true);
            }

            foreach (Colorable co in colorableObjects)
            {
                co.ResetBase();
                yield return null;
            }
        }
        resetting = false;
    }

    public IEnumerator Screenshot()
    {
        string fullPath = System.IO.Directory.GetCurrentDirectory() + "\\Screenshots\\";

        if (!System.IO.Directory.Exists(fullPath))
            System.IO.Directory.CreateDirectory(fullPath);

        string fileName = "TIAF-Screenshot";
        int i = 1;
        while (System.IO.File.Exists(fullPath + fileName + "-" + i + ".png"))
        {
            i++;
            yield return null;
        }
        Application.CaptureScreenshot(fullPath + fileName + "-" + i + ".png");
    }

    private IEnumerator DisplayMenu()
    {
        menuDisplaying = true;

        // generate menu orbs
        for (int i = 0; i < menuItems.Length; i++)
        {
            menuItems[i] = Instantiate(menuObject);
            menuItems[i].transform.SetParent(transform);
            menuItems[i].name = "Menu " + i;
            yield return null;
        }

        // animate menu orbs
        float deltat = 0;
        float lt = 0;
        float transtime = .75f; // how long the transition takes

        while (menuDisplaying)
        {
            menuItems[0].transform.localPosition = new Vector3(Mathf.Lerp(0, 0.09f, lt), Mathf.Lerp(-0.05f, -0.21f, lt), Mathf.Lerp(0, -0.02f, lt));
            menuItems[0].transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.1f, lt);
            menuItems[1].transform.localPosition = new Vector3(Mathf.Lerp(0,     0f, lt), Mathf.Lerp(-0.05f, -0.27f, lt), Mathf.Lerp(0,  0.04f, lt));
            menuItems[1].transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.0f, lt);
            menuItems[2].transform.localPosition = new Vector3(Mathf.Lerp(0, -0.04f, lt), Mathf.Lerp(-0.05f, -0.14f, lt), Mathf.Lerp(0, 0.11f, lt));
            menuItems[2].transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, lt);

            deltat += Time.deltaTime;
            lt = deltat / transtime;
            lt = lt * lt * lt * (lt * (6f * lt - 15f) + 10f);

            yield return null;
        }

    }

    private IEnumerator HideMenu()
    {
        menuDisplaying = false;

        // animate menu orbs
        float deltat = 0;
        float lt = 0;
        float transtime = .35f; // how long the transition takes

        Vector3[] menuPos = new Vector3[menuItems.Length];
        for (int i = 0; i < menuItems.Length; i++)
        {
            menuPos[i] = menuItems[i].transform.localPosition;
        }

        while (lt < 1)
        {
            menuItems[0].transform.localPosition = new Vector3(Mathf.Lerp(menuPos[1].x, 0, lt), Mathf.Lerp(menuPos[1].y, -0.05f, lt), Mathf.Lerp(menuPos[1].z, 0, lt));
            menuItems[0].transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.3f, lt);
            menuItems[1].transform.localPosition = new Vector3(Mathf.Lerp(menuPos[0].x, 0, lt), Mathf.Lerp(menuPos[0].y, -0.05f, lt), Mathf.Lerp(menuPos[0].z, 0, lt));
            menuItems[1].transform.localScale = Vector3.one * Mathf.Lerp(1, 0.3f, lt);
            menuItems[2].transform.localPosition = new Vector3(Mathf.Lerp(menuPos[2].x, 0, lt), Mathf.Lerp(menuPos[2].y, -0.05f, lt), Mathf.Lerp(menuPos[2].z, 0, lt));
            menuItems[2].transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.3f, lt);

            deltat += Time.deltaTime;
            lt = deltat / transtime;
            lt = lt * lt * lt * (lt * (6f * lt - 15f) + 10f);

            yield return null;
        }

        for (int i = 0; i < menuItems.Length; i++)
        {
            Destroy(menuItems[i]);
            yield return null;
        }

    }
}
