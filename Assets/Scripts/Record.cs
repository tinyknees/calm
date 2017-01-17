﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using GameSparks.Api.Requests;
using GameSparks.Api.Messages;
using GameSparks.Core;

[RequireComponent(typeof(ControllerEvents))]

public class Record : MonoBehaviour {
    public GameObject audioContainer;
    private ControllerEvents controllerEvents; // the controller where event happened
    private ControllerEvents.ControllerInteractionEventArgs activeController;
    private AudioSource recordsource;

    [Tooltip("Default color of the record button when inactive.")]
    public Color recorderColor; 
    [Tooltip("Krista: 'Because I'm annoyign'")]
    public Color recordButtonColor = Color.red;
    [Tooltip("How many steps to pulse. Higher is slower fade.")]
    public float pulseSteps = 50f;

    [HideInInspector]
    public bool startRecording = false; // external flag for 

    [Range(0, 3f)]
    [Tooltip("Distance to objects before recording is allowed.")]
    public float distanceThreshold = 1.6f;

    private GameObject lastNearestQuote = null;

    [HideInInspector]
    public GameObject recordObject = null;


    private bool touchpadUpPressed = false;
    private bool touchpadReleased = false;
    private bool menuReleased = false;

    private string playerId = "";
    private string lastUploadId = "";
    private bool initializingGS = false;
    private bool requestingAudio = false;

    private AudioMixer mrmrMixer;
    private GameObject[] allQuoteObjects;

    private Transform recordButton; // Easily access the record button to change its colour
    private bool canRecord = false; // Are you close enough, quote revealed, have you recorded?

    private bool requestedAllAudio = false;
    private bool playingAudio = false;
    private bool timerUp = false;

    [Range(0,1)]
    public float defaultQuoteVolume = 0.6f;

    private Dictionary<string, List<AudioSource>> allAudio = new Dictionary<string, List<AudioSource>>();

    void Awake()
    {
        UploadCompleteMessage.Listener += GetUploadMessage;

        if (audioContainer == null)
        {
            audioContainer = new GameObject();
            audioContainer.name = "Recording Container";
        }
        if (audioContainer.GetComponent<AudioSource>() == null)
        {
            audioContainer.AddComponent<AudioSource>();
        }
        recordsource = audioContainer.GetComponent<AudioSource>();

        mrmrMixer = Resources.Load("Mrmrs") as AudioMixer;
        

        controllerEvents = GetComponent<ControllerEvents>();

        allQuoteObjects = GameObject.FindGameObjectsWithTag("Quote");

        recordButton = gameObject.transform.FindChild("Pencil").FindChild("Record");
        recordButton.GetComponent<Renderer>().material.color = recorderColor;

        gameObject.transform.FindChild("ConsoleViewerCanvas").gameObject.SetActive(false);

    }

    void InitGS()
    {
        initializingGS = true;
        // authenticate
        new DeviceAuthenticationRequest().Send((response) =>
        {
            if (!response.HasErrors)
            {
                playerId = response.UserId;
                Debug.Log("Device Authenticated..." + playerId);
            }
            else
            {
                Debug.Log("Error Authenticating Device...");
            }
            initializingGS = false;
        });
    }

    void OnEnable()
    {
        controllerEvents.TouchpadUpPressed += HandleTouchpadUpPressed;
        controllerEvents.MenuPressed += HandleMenuPressed;
        controllerEvents.MenuReleased += HandleMenuReleased;
        controllerEvents.TouchpadReleased += HandleTouchpadReleased;
    }
    void OnDisable()
    {
        controllerEvents.TouchpadUpPressed -= HandleTouchpadUpPressed;
        controllerEvents.MenuPressed -= HandleMenuPressed;
        controllerEvents.MenuReleased -= HandleMenuReleased;
        controllerEvents.TouchpadReleased -= HandleTouchpadReleased;
    }

    // Update is called once per frame
    void Update ()
    {
        if (playerId != "")
        {
            // Start downloading audio when we have an active controller to do recordings, etc.
            if (gameObject.activeSelf && !requestingAudio && !requestedAllAudio)
            {
                RequestAllAudio();
            }

            if (menuReleased && // pressed the button
                !Microphone.IsRecording(null) && // not recording
                canRecord // fits criteria to be recording
                )
            {
                menuReleased = false;
                StartCoroutine(ChangeVolume(0.2f));
                recordButton.GetComponent<AudioSource>().clip = (AudioClip)Resources.Load("startrecord");
                recordButton.GetComponent<AudioSource>().Play();

                Debug.Log("Started Recording for: " + recordObject.name);

                recordsource.clip = Microphone.Start(null, true, 45, 44100);
                StartCoroutine(RecordingCounter());
            }
            else if (timerUp || // was recording and timer ran out
                (Microphone.IsRecording(null) && menuReleased) // currently recording and press menu to stop
                )
            {
                menuReleased = false;
                timerUp = false;
                StartCoroutine(ChangeVolume(defaultQuoteVolume));
                Debug.Log("Stopped Recording " + recordsource.clip.samples);
                gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().appMenuText = "Saving…";

                if (recordsource.clip.samples > 0)
                {
                    recordObject.GetComponentInChildren<Quote>().recorded = true;
                    ToggleRecordButton(false);

                    Save("tiaf", recordsource.clip);
                }
                Microphone.End(null);
            }


            if (touchpadReleased && touchpadUpPressed)
            {
                GameObject cv = gameObject.transform.FindChild("ConsoleViewerCanvas").gameObject;
                cv.SetActive(!cv.activeSelf);
                touchpadReleased = false;
                touchpadUpPressed = false;
            }

            CheckQuoteDistance();

            if (!playingAudio && requestedAllAudio)
            {
                StartCoroutine(PlayAudio());
            }
        }
        else if (!initializingGS)
        {
            InitGS();
        }

    }

    private IEnumerator RecordingCounter ()
    {
        float start = Time.time;
        double timer = 45;
        string counter = "-00:";

        timerUp = false;
        gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().ToggleTips(true, VRTK.VRTK_ControllerTooltips.TooltipButtons.AppMenuTooltip);

        while (Microphone.IsRecording(null) && timer > 0)
        {
            timer = Math.Round(45 - (Time.time - start));
            if (timer < 10)
            {
                counter = "-00:0" + timer.ToString();
            }
            else
            {
                counter = "-00:" + timer.ToString();
            }
            gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().appMenuText = counter;
            yield return null;
        }

        if (timer <= 0)
            timerUp = true;
    }

    /// <summary>
    /// Turns on or off the recording button and ability to record
    /// </summary>
    /// <param name="on">true = turn on recording, false = turn off</param>
    private void ToggleRecordButton (bool on)
    {
        if (recordObject != null && on)
        {
            canRecord = true;
            StartCoroutine("PulseMaterial", recordButtonColor);
            bool pinged = lastNearestQuote.GetComponentInChildren<Quote>().pinged;
            if (!pinged)
            {
                gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().appMenuText = "Press to record";
                recordButton.GetComponent<AudioSource>().clip = (AudioClip) Resources.Load("recordprompt");
                recordButton.GetComponent<AudioSource>().Play();
                gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().ToggleTips(true, VRTK.VRTK_ControllerTooltips.TooltipButtons.AppMenuTooltip);
                pinged = true;
            }
        }
        else
        {
            canRecord = false;
            StopCoroutine("PulseMaterial");
            recordButton.GetComponent<Renderer>().material.color = recorderColor;
            gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().ToggleTips(false, VRTK.VRTK_ControllerTooltips.TooltipButtons.AppMenuTooltip);
        }
    }

    const int HEADER_SIZE = 44;

    bool Save(string filename, AudioClip clip)
    {
        if (!filename.ToLower().EndsWith(".wav"))
        {
            filename += ".wav";
        }

        var filepath = Path.Combine(Application.persistentDataPath, filename);

        Debug.Log(filepath);

        // Make sure directory exists if user is saving to sub dir.
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        clip = TrimSilence(clip, 0.01f);

        using (var fileStream = CreateEmpty(filepath))
        {

            ConvertAndWrite(fileStream, clip);

            WriteHeader(fileStream, clip);
        }
        Debug.Log("filepath: " + filepath);
        UploadAudio(filename, File.ReadAllBytes(filepath));

        return true; // TODO: return false if there's a failure saving the file
    }

    AudioClip TrimSilence(AudioClip clip, float min)
    {
        var samples = new float[clip.samples*clip.channels];

        clip.GetData(samples, 0);

        return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
    }

    AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
    {
        return TrimSilence(samples, min, channels, hz, false);
    }

    AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool stream)
    {
        int i;

        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(0, i);

        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(i, samples.Count - i);

        var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, stream);

        clip.SetData(samples.ToArray(), 0);

        return clip;
    }

    FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }


    void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {

        var samples = new float[clip.samples];

        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

        Byte[] bytesData = new Byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        float rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fileStream, AudioClip clip)
    {

        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        //UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        //		fileStream.Close();
    }

    private void UploadAudio(string filename, Byte[] data)
    {
        Debug.Log("uploading");

        GSRequestData gsdata = new GSRequestData().AddString("Quote",recordObject.name);

        new GetUploadUrlRequest().SetUploadData(gsdata).Send((response) =>
        {
            //Start coroutine and pass in the upload url
            StartCoroutine(UploadAFile(response.Url, filename, data));
        });
    }


    //Our coroutine takes the upload url
    private IEnumerator UploadAFile(string uploadUrl, string filename, Byte[] data)
    {
        Debug.Log("uploadurl " + uploadUrl);
        Debug.Log("data size " + data.Length);

        // Create a Web Form, this will be our POST method's data
        var form = new WWWForm();
        form.AddBinaryData("file", data, filename, "audio/wav");

        gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().appMenuText = "Uploading…";

        WWW w = new WWW(uploadUrl, form);
        while (!w.isDone)
        {
            yield return w;
        }

        if (w.error != null)
        {
            Debug.Log(w.error);
        }
        else
        {
            Debug.Log(w.text);
        }
        gameObject.GetComponentInChildren<VRTK.VRTK_ControllerTooltips>().ToggleTips(false, VRTK.VRTK_ControllerTooltips.TooltipButtons.AppMenuTooltip);

    }


    private void RequestAllAudio()
    {
        string quotename = "";
        requestingAudio = true;

        //download all files
        new LogEventRequest().SetEventKey("LOAD_AUDIO").Send((response) =>
        {
            if (!response.HasErrors)
            {
                GSData data = response.ScriptData;
                int i = 0;
                if (data != null)
                {
                    string uploadId = data.GetString("uploadId" + i);
                    quotename = data.GetString("Quote" + i);

                    while (uploadId != null)
                    {
                        Debug.Log(uploadId + " / " + quotename);

                        if (!allAudio.ContainsKey(quotename))
                        {
                            allAudio.Add(quotename, new List<AudioSource>());
                        }
                        allAudio[quotename].Add(null);
                        DownloadAFile(uploadId, quotename);

                        i++;
                        uploadId = data.GetString("uploadId" + i);
                        quotename = data.GetString("Quote" + i);
                    }

                    requestedAllAudio = true;
                }
                requestingAudio = false;
            }
        });

    }


    // Download a file from GameSparks given an uploadId
    private void DownloadAFile(string uploadId = "", string quoteobject = "")
    {
        if (uploadId == "")
        {
            uploadId = lastUploadId;
        }
        //Get the url associated with the uploadId
        new GetUploadedRequest().SetUploadId(uploadId).Send((response) =>
        {
            //pass the url to our coroutine that will accept the data
            StartCoroutine(DownloadAudio(response.Url, quoteobject));
        });
    }

    private IEnumerator DownloadAudio(string downloadUrl, string quotename)
    {
        WWW www = new WWW(downloadUrl);

        while (!www.isDone)
        {
            Debug.Log("Downloading: " + downloadUrl);
            yield return www;
        }

        GameObject[] quotes = GameObject.FindGameObjectsWithTag("Quote");
        foreach (GameObject quote in quotes)
        {
            if (quote.name == quotename)
            {
                AudioSource quoteaudio = quote.AddComponent<AudioSource>();
                quoteaudio.spatialBlend = 1.0f;
                quoteaudio.volume = defaultQuoteVolume;
                quoteaudio.rolloffMode = AudioRolloffMode.Linear;
                quoteaudio.minDistance = 0.3f;
                quoteaudio.maxDistance = 3.5f;
                quoteaudio.clip = www.GetAudioClip(false, false, AudioType.WAV);
                quoteaudio.outputAudioMixerGroup = mrmrMixer.FindMatchingGroups("Mrmrs")[0];

                
                if (allAudio.ContainsKey(quotename))
                {
                    int i = 0;
                    while (allAudio[quotename][i] != null)
                        i++;
                    allAudio[quotename][i] = quoteaudio;
                    Debug.Log(quotename + " added to " + i);
                }

            }
        }

    }

    //This will be our message listener
    public void GetUploadMessage(GSMessage message)
    {
        //Every time we get a message
        Debug.Log(message.BaseData.GetString("uploadId"));
        //Save the last uploadId
        lastUploadId = message.BaseData.GetString("uploadId");
    }

    // Runs through all the downloaded recordings and sequences them to play in a loop per object
    private IEnumerator PlayAudio()
    {
        playingAudio = true; // global flag to inform if this coroutine is running
        Dictionary<string, List<int>> rando = new Dictionary<string, List<int>>();
        Dictionary<string, int> next = new Dictionary<string, int>();
        Dictionary<string, int> play = new Dictionary<string, int>();

        // set up full dictionary and randomize here
        foreach (GameObject qc in allQuoteObjects)
        {
            next.Add(qc.name, 0);
            play.Add(qc.name, 0);
            rando.Add(qc.name, new List<int>());
            Debug.Log(qc.name);
            for (int j = 0; j < allAudio[qc.name].Count; j++)
            {
                rando[qc.name].Add(j);
            }
            for (int j = 0; j < rando[qc.name].Count; j++)
            {
                int tmp = rando[qc.name][j];
                int r = UnityEngine.Random.Range(j, rando[qc.name].Count);
                rando[qc.name][j] = rando[qc.name][r];
                rando[qc.name][r] = tmp;
            }
        }
        foreach (GameObject qc in allQuoteObjects)
        {
            foreach (int k in rando[qc.name])
               Debug.Log("rando: " + qc.name +"/" + rando[qc.name][k]);
        }


        while (true)
        {
            foreach (GameObject qc in allQuoteObjects)
            {
                int n = next[qc.name];
                int p = play[qc.name];
                int r = rando[qc.name][n];
                AudioSource aus = allAudio[qc.name][p];

                // move current up
                if (allAudio[qc.name][p] != null)
                {
                    // the one to play next is the current one and it's not playing so play
                    if (!aus.isPlaying)
                    {
                        if (n == p)
                        {
                            Debug.Log("playing: " + qc.name + " " + r + "/" + n);
                            aus.Play();

                            // move up play next
                            n = (n >= allAudio[qc.name].Count - 1) ? 0 : n + 1;
                            next[qc.name] = n;
                        }
                        // the one playing is stopped, time to move to next playing
                        else
                        {
                            play[qc.name] = (p >= allAudio[qc.name].Count - 1) ? 0 : p + 1;
                        }
                    }
                    // if it's playing, don't do anytthing
                    else
                    {
                    }
                }
                // if the one we're suppose to play is null, bump everything up
                else
                {
                    play[qc.name] = (p >= allAudio[qc.name].Count - 1) ? 0 : p + 1;
                    next[qc.name] = (n >= allAudio[qc.name].Count - 1) ? 0 : n + 1;
                }
            }
            yield return null;
        }
    }

    private IEnumerator ChangeVolume(float vol)
    {
        foreach (GameObject qc in allQuoteObjects)
        {
            AudioSource[] auss = qc.GetComponents<AudioSource>();
            foreach (AudioSource aus in auss)
            {
                if (aus.outputAudioMixerGroup == null && vol == defaultQuoteVolume)
                    aus.volume = vol * 1.2f;
                else
                    aus.volume = vol;
                yield return null;
            }
        }
    }

    // Check which quote player is nearest and also turn things on or off based on distance
    private void CheckQuoteDistance()
    {
        float dist;
        float nearestDist = 100f;
        GameObject nearestQuote = null;

        foreach (GameObject qc in allQuoteObjects)
        {
            if (qc != null)
            {
                dist = Vector3.Distance(gameObject.transform.position, qc.transform.position);

                // walked away from last quote playing so turn murmurs back on and disable recording
                if (dist >= distanceThreshold && lastNearestQuote == qc)
                {
                    AudioSource[] auss = qc.GetComponents<AudioSource>();
                    foreach (AudioSource aus in auss)
                    {
                        aus.outputAudioMixerGroup = mrmrMixer.FindMatchingGroups("Mrmrs")[0];
                        aus.volume = defaultQuoteVolume;
                    }

                    ToggleRecordButton(false);
                }

                // set new neearest quote if applicable
                if (nearestQuote == null || nearestDist > dist)
                {
                    nearestDist = dist;
                    nearestQuote = qc;
                }
            }
        }

        lastNearestQuote = nearestQuote;

        // if close enough, check if quote is revealed
        if (nearestDist < distanceThreshold)
        {
            Quote quote = nearestQuote.GetComponentInChildren<Quote>();
            if (quote != null)
            {
                if (quote.revealed)
                {
                    // turn recording back on if quote hasn't been recorded
                    if (!quote.recorded)
                    {
                        if (!canRecord)
                        {
                            ToggleRecordButton(true);
                        }
                        recordObject = nearestQuote;
                    }

                    // make recorded quotes clear as you get close
                    AudioSource[] auss = nearestQuote.GetComponents<AudioSource>();
                    foreach (AudioSource aus in auss)
                    {
                        if (!Microphone.IsRecording(null))
                            aus.volume = defaultQuoteVolume * 1.2f;
                        aus.outputAudioMixerGroup = null;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Pulses a given material's color from default to parameter while microphone is not recording.
    /// </summary>
    /// <param name="fc">Color to fade to</param>
    // TODO: set as multi-param allowing for steps and to color and use a flag to stop coroutine
    private IEnumerator PulseMaterial(Color fc)
    {
        float t = 0;
        bool pulseup = true;

        while (!Microphone.IsRecording(null))
        {
            recordButton.GetComponent<Renderer>().material.color = Color.Lerp(fc, recorderColor, t);

            t = (pulseup) ? t = t + 1 / pulseSteps : t = t - 1 / pulseSteps;

            if (t > 1)
                pulseup = false;
            else if (t < 0)
                pulseup = true;

            yield return null;
        }
        recordButton.GetComponent<Renderer>().material.color = fc;
    }


    
    /* EVENT HANDLERS ----------------------------------------------------------------------*/

    private void HandleTouchpadUpPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadUpPressed = true;
    }

    private void HandleMenuPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
    }

    private void HandleMenuReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        menuReleased = true;
    }

    private void HandleTouchpadReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadReleased = true;
   }


}