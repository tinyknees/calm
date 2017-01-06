using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameSparks.Api.Requests;
using GameSparks.Api.Messages;
using GameSparks.Core;

[RequireComponent(typeof(ControllerEvents))]

public class Record : MonoBehaviour {
    public GameObject audioContainer;
    private ControllerEvents controllerEvents; // the controller where event happened
    private ControllerEvents.ControllerInteractionEventArgs activeController;
    private AudioSource audiosource;
    private bool startRecording = false;
    private bool startPlaying = false;

    [HideInInspector]
    public GameObject recordObject = null;
    private bool touchpadUpPressed = false;
    private bool menuPressed = false;
    private bool touchpadReleased = false;

    private string playerId;
    private string lastUploadId;

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
        audiosource = audioContainer.GetComponent<AudioSource>();

        controllerEvents = GetComponent<ControllerEvents>();

        Invoke("InitGS", 3f);
    }

    void InitGS()
    {
        // authenticate
        new DeviceAuthenticationRequest().Send((response) =>
        {
            if (!response.HasErrors)
            {
                Debug.Log("Device Authenticated...");
                playerId = response.UserId;
            }
            else
            {
                Debug.Log("Error Authenticating Device...");
            }
        });

        //download all files
        new LogEventRequest().SetEventKey("LOAD_AUDIO").Send((response) =>
        {
            if (!response.HasErrors)
            {
                GSData data = response.ScriptData;
                int i = 0;
                String uploadId = data.GetString("uploadId" + i);
                String quoteObject = data.GetString("Quote" + i);
                while (uploadId != null)
                {
                    DownloadAFile(uploadId, quoteObject);
                    Debug.Log(uploadId + " / " + quoteObject);
                    i++;
                    uploadId = data.GetString("uploadId" + i);
                    quoteObject = data.GetString("Quote" + i);
                }
            }
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
        if (startRecording)
        {
            startRecording = false;
            if (!Microphone.IsRecording(null))
            {
                Debug.Log("Started Recording for: " + recordObject.name);
                audiosource.clip = Microphone.Start(null, true, 45, 44100);
            }
            else
            {
                Debug.Log("Stopped Recording " + audiosource.clip.samples);

                if (audiosource.clip.samples > 0)
                {
                    Save("recordingcalm", audiosource.clip);
                }
                Microphone.End(null);
            }
        }

        if (touchpadReleased)
        {
            if (touchpadUpPressed)
            {
                if (startPlaying)
                {
                    if (!audiosource.isPlaying)
                    {
                        Debug.Log("Started Playback");
                        DownloadAFile();
//                        audiosource.Play();
                    }
                }
                else
                {
                    Debug.Log("Stopped Playback");
                    audiosource.Stop();
                }
                touchpadUpPressed = false;
            }

            touchpadReleased = false;

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

        using (var fileStream = CreateEmpty(filepath))
        {

            ConvertAndWrite(fileStream, clip);

            WriteHeader(fileStream, clip);
        }
        Debug.Log("filepath: " + filepath);
        UploadAudio(File.ReadAllBytes(filepath));

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

        UInt16 two = 2;
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

    private void UploadAudio(Byte[] data)
    {
        Debug.Log("uploading");

        GSRequestData gsdata = new GSRequestData().AddString("Quote",recordObject.name);

        new GetUploadUrlRequest().SetUploadData(gsdata).Send((response) =>
        {
            //Start coroutine and pass in the upload url
            StartCoroutine(UploadAFile(response.Url, data));
        });
    }


    //Our coroutine takes the upload url
    private IEnumerator UploadAFile(string uploadUrl, Byte[] data)
    {
        Debug.Log("uploadurl " + uploadUrl);
        Debug.Log("data size " + data.Length);

        // Create a Web Form, this will be our POST method's data
        var form = new WWWForm();
        form.AddBinaryData("file", data, "calm.wav", "audio/wav");

        WWW w = new WWW(uploadUrl, form);
        yield return w;

        if (w.error != null)
        {
            Debug.Log(w.error);
        }
        else
        {
            Debug.Log(w.text);
        }
    }

    //When we want to download our uploaded image
    private void DownloadAFile(string uploadId = "", String quoteobject = "")
    {
        if (uploadId == "")
        {
            uploadId = lastUploadId;
        }
        //Get the url associated with the uploadId
        new GetUploadedRequest().SetUploadId(uploadId).Send((response) =>
        {
            //pass the url to our coroutine that will accept the data
            StartCoroutine(PlayAudio(response.Url, quoteobject));
        });
    }


    private IEnumerator PlayAudio(string downloadUrl, string quoteobject)
    {
        var www = new WWW(downloadUrl);
        while (!www.isDone)
        {
            Debug.Log("Downloading: " + downloadUrl);
            yield return www;
        }

        Debug.Log(quoteobject);
        GameObject[] quotes = GameObject.FindGameObjectsWithTag("Quote");
        foreach (GameObject quote in quotes)
        {
            if (quote.name == quoteobject)
            {
                AudioSource quoteaudio = quote.AddComponent<AudioSource>();
                quoteaudio.spatialBlend = 1.0f;
                quoteaudio.loop = true;
                quoteaudio.clip = www.GetAudioClip(false, false, AudioType.WAV);
                quoteaudio.Play();
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

    private void HandleTouchpadUpPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadUpPressed = true;
    }

    private void HandleMenuPressed(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        Debug.Log("menu pressed");
    }

    private void HandleMenuReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        if (recordObject != null)
        {
            startRecording = startRecording ? false : true;
        }
    }

    private void HandleTouchpadReleased(object sender, ControllerEvents.ControllerInteractionEventArgs e)
    {
        touchpadReleased = true;

        if (touchpadUpPressed)
        {
            startPlaying = startPlaying ? false : true;
        }
    }


}