
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class RiddlerManager : Singleton<RiddlerManager>
{
    public List<Riddle> riddles = new List<Riddle>();
    public int currentRiddle = 0;

    //RecordClip
    public AudioClip recordedClip;
    public AudioClip trimmedClip;
    private bool isRecording = false;
    public string deviceName;
    private int frequency = 16000;  // Standard sample rate
    private int recordingLength = 300;  // Maximum recording length in seconds

    [SerializeField] private AudioSource audioSource;


    private void Awake()
    {
     //   currentRiddle = 0;
    }
    void Start()
    {

    }

    public async void RequestRiddle()
    {
       
        // Debug.Log("RequestRiddle");
        Riddle r = riddles[currentRiddle];
        string q = r.Question;
        RiddlerUIManager.Instance.SetRiddle(q);
        RiddlerUIManager.Instance.SetAnswer("");
        //
        TextToSpeech.Instance.SetText(q);

        if (Application.isEditor && !Application.isPlaying)
            TextToSpeech.Instance.SetupInferenceEngine();

        await TextToSpeech.Instance.Run();

    }
    public void AnswerRiddle()
    {
       SpeechToText.Instance.SetAudioClip(recordedClip);
    }

    public void NextRiddle()
    {
        currentRiddle++;
        if (currentRiddle >= riddles.Count)
            currentRiddle = 0;

        RequestRiddle();
    }
    public void ResetRiddles()
    {
        currentRiddle = 0;

        RequestRiddle();
    }
    public void PrevRiddle()
    {
        currentRiddle--;
        if (currentRiddle < 0)
            currentRiddle = riddles.Count - 1;

        RequestRiddle();
    }

    public void SetMic(string s)
    {
        Debug.Log("SetMic " + s);
        deviceName = s;
    }
    public void RecordAudio(bool isRecording)
    {
  
        if (isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
            AnswerRiddle();
            // SaveRecording();
           // PlayRecording();

        }
    }

    public void StartRecording()
    {
       // Debug.Log("StartRecording " + isRecording + " devicename" + deviceName);
        if (!isRecording && deviceName != null)
        {
            Debug.Log("Starting Recording...");
            if (recordedClip)
            {
                Destroy(recordedClip);
            }
            recordedClip = Microphone.Start(deviceName, false, recordingLength, frequency);
            isRecording = true;
        }
    }

    public void StopRecording()
    {
        //Debug.Log("StopRecording");
        if (isRecording)
        {
            Debug.Log("Stopping Recording...");

            // Get the recording position before stopping
            int position = Microphone.GetPosition(deviceName);
            Microphone.End(deviceName);

            // Trim the audioclip to the actual recorded length
            if (position > 0)
            {
                var samples = new NativeArray<float>(position * recordedClip.channels, Allocator.Temp);
               
                recordedClip.GetData(samples, 0);
                if(trimmedClip)
                    AudioClip.Destroy(trimmedClip);
                trimmedClip = AudioClip.Create(
                    "Recorded",
                    position,
                    recordedClip.channels,
                    frequency,
                    false
                );
                trimmedClip.SetData(samples, 0);
                recordedClip = trimmedClip;

                samples.Dispose();
               
            }

            isRecording = false;
        }
        else
        {
            Debug.Log("was not recording");
        }
    }



    public void PlayRecording()
    {
        if (recordedClip != null && audioSource != null)
        {
            audioSource.clip = recordedClip;
            audioSource.Play();
        }
    }

    public void SaveRecording()
    {

        if (recordedClip != null)
        {
            // Convert AudioClip to WAV format
            byte[] wavData = AudioClipToWav(recordedClip);

            // Save to persistent data path with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
           // string filePath = $"{Application.streamingAssetsPath}/Recording_{timestamp}.wav";
            string filePath = $"{Application.streamingAssetsPath}/CurrentRecording.wav";

            try
            {
                System.IO.File.WriteAllBytes(filePath, wavData);
                Debug.Log($"Recording saved to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save recording: {e.Message}");
            }
        }
        else
        {

            Debug.Log("recordedClip is null");
        }
    }

    private byte[] AudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];

        // Convert float to Int16
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
        }

        byte[] bytesData = new byte[intData.Length * 2];
        Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

        // Create WAV file header
        byte[] header = CreateWavHeader(bytesData.Length, clip.channels, clip.frequency);

        // Combine header and audio data
        byte[] wavFile = new byte[header.Length + bytesData.Length];
        Buffer.BlockCopy(header, 0, wavFile, 0, header.Length);
        Buffer.BlockCopy(bytesData, 0, wavFile, header.Length, bytesData.Length);

        return wavFile;
    }

    private byte[] CreateWavHeader(int dataSize, int channels, int sampleRate)
    {
        byte[] header = new byte[44];

        // RIFF header
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
        BitConverter.GetBytes(dataSize + 36).CopyTo(header, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(header, 8);

        // Format chunk
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(header, 12);
        BitConverter.GetBytes(16).CopyTo(header, 16);
        BitConverter.GetBytes((short)1).CopyTo(header, 20);
        BitConverter.GetBytes((short)channels).CopyTo(header, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(header, 24);
        BitConverter.GetBytes(sampleRate * channels * 2).CopyTo(header, 28);
        BitConverter.GetBytes((short)(channels * 2)).CopyTo(header, 32);
        BitConverter.GetBytes((short)16).CopyTo(header, 34);

        // Data chunk
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
        BitConverter.GetBytes(dataSize).CopyTo(header, 40);

        return header;
    }
    public bool GetIsRecording() { return isRecording; }
    public bool GetIsPaused() { return audioSource.isPlaying; }
    public void SetIsRecording(bool b) { isRecording = b; }
    public void StopReplay()
    {
        if (audioSource != null && audioSource.isPlaying)
        {

            audioSource.Stop();
        }
    }
    public void PauseReplay()
    {
        if (audioSource != null)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
            else
            {
                audioSource.UnPause();
            }
        }

    }
    public async void CheckAnswer(string s)
    {
        Debug.Log("CheckAnswer " + s);

        Riddle r = riddles[currentRiddle];
        string result = string.Empty;
        if (s.ToLower().Contains(r.Answer.ToLower()))
        {
            result = "Correct: " + s;
        }
        else
        {
            result = s + " Wrong try again!";
        }
        RiddlerUIManager.Instance.SetAnswer(result);

        TextToSpeech.Instance.SetText(result);
        await TextToSpeech.Instance.Run();
    }
}
