using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.InferenceEngine;
using System.Collections;
using UnityEngine.Networking;
using Unity.Profiling;
using System.Net;

public class TextToSpeech : Singleton<TextToSpeech>
{
    static readonly ProfilerMarker TextToSpeechRunMarker = new ProfilerMarker("TextToSpeech::Run");
    static readonly ProfilerMarker PeekpMarker = new ProfilerMarker("TextToSpeech::Peek");
    static readonly ProfilerMarker CloneMarker = new ProfilerMarker("TextToSpeech::Clone");
    static readonly ProfilerMarker DownloadMarker = new ProfilerMarker("TextToSpeech::Download");
    public string inputText = "Once upon a time, there lived a girl called Alice. She lived in a house in the woods.";
    //string inputText = "The quick brown fox jumped over the lazy dog";
    //string inputText = "There are many uses of the things she uses!";

    //Set to true if we have put the phoneme_dict.txt in the Assets/StreamingAssets folder
    bool hasPhenomeDictionary = true;

    readonly string[] phonemes = new string[] {
        "<blank>", "<unk>", "AH0", "N", "T", "D", "S", "R", "L", "DH", "K", "Z", "IH1",
        "IH0", "M", "EH1", "W", "P", "AE1", "AH1", "V", "ER0", "F", ",", "AA1", "B",
        "HH", "IY1", "UW1", "IY0", "AO1", "EY1", "AY1", ".", "OW1", "SH", "NG", "G",
        "ER1", "CH", "JH", "Y", "AW1", "TH", "UH1", "EH2", "OW0", "EY2", "AO0", "IH2",
        "AE2", "AY2", "AA2", "UW0", "EH0", "OY1", "EY0", "AO2", "ZH", "OW2", "AE0", "UW2",
        "AH2", "AY0", "IY2", "AW2", "AA0", "\"", "ER2", "UH2", "?", "OY2", "!", "AW0",
        "UH0", "OY0", "..", "<sos/eos>" };

    readonly string[] alphabet = "AE1 B K D EH1 F G HH IH1 JH K L M N AA1 P K R S T AH1 V W K Y Z".Split(' ');

    //Can change pitch and speed with this for a slightly different voice:
    public int samplerate = 22050;
    public AudioSource audioSource;
    public float clipLength = 0;

    [Header("Model")]
    public ModelAsset modelAsset = null;
    Dictionary<string, string> dict = new();

    Worker engine;
    Model model;
    AudioClip clip;


    private bool m_IsExecuting;
    public float timeThreshold = 0.004f; // 16ms
    void Start()
    {
        SetupInferenceEngine();
    }
    public void SetupInferenceEngine()
    {
        Debug.Log("SetupInferenceEngine");
        LoadModel();
        ReadDictionary();
    }

  

    void LoadModel()
    {
    
        if (model == null)
            model = ModelLoader.Load(modelAsset);

        if ( engine == null )
            engine = new Worker(model,BackendType.CPU);
 
    }
    public void SetText(string s)
    {
        inputText = s;
    }
    public async Awaitable Run()
    {
        TextToSpeechRunMarker.Begin();
        string ptext;
        if (hasPhenomeDictionary)
        {
            ptext = TextToPhonemes(inputText);
            //Debug.Log(ptext);
        }
        else
        {
            //If we have no phenome dictionary we can use one of these examples:
            ptext = "DH AH0 K W IH1 K B R AW1 N F AA1 K S JH AH1 M P S OW1 V ER0 DH AH0 L EY1 Z IY0 D AO1 G .";
            //ptext = "W AH1 N S AH0 P AA1 N AH0 T AY1 M , AH0 F R AA1 G M EH1 T AH0 P R IH1 N S EH0 S . DH AH0 F R AA1 G K IH1 S T DH AH0 P R IH1 N S EH0 S AH0 N D B IH0 K EY1 M AH0 P R IH1 N S .";
            //ptext = "D UW1 P L AH0 K EY2 T";
        }
        TextToSpeechRunMarker.End();
       // StartCoroutine(ExecuteDoInferenceTimeSlicing(ptext));
        await DoInferenceASync(ptext);
        
    }

    async void ReadDictionary()
    {
        dict.Clear();
        if (!hasPhenomeDictionary) return;

       // StartCoroutine(LoadPhoneme("phoneme_dict.txt", (lines) => AddPhoneme(lines)));

        await LoadPhonemeAsync("phoneme_dict.txt", (lines) => AddPhoneme(lines));

    }
    public async Awaitable LoadPhonemeAsync(string fileName, System.Action<string[]> onComplete)
    {
        Debug.Log("LoadPhonemeAsync");
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);


        // On Android, we must use UnityWebRequest to access StreamingAssets
        UnityWebRequest www = UnityWebRequest.Get(filePath);
        var asyncOp = www.SendWebRequest();
        while (!asyncOp.isDone)
        {
            await Awaitable.NextFrameAsync();
        }

        string textContent = www.downloadHandler.text;


        // Split the text content into lines
        string[] lines = textContent.Split(new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        onComplete?.Invoke(lines);
    }
    public IEnumerator LoadPhoneme(string fileName, System.Action<string[]> onComplete)
    {
        Debug.Log("LoadPhoneme");
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);


            // On Android, we must use UnityWebRequest to access StreamingAssets
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load file: {www.error}");
                onComplete?.Invoke(null);
                yield break;
            }
            
            string textContent = www.downloadHandler.text;


        // Split the text content into lines
        string[] lines = textContent.Split(new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        onComplete?.Invoke(lines);
    }
    private void AddPhoneme(string[] words)
    {

        Debug.Log("AddPhoneme");
        //string[] words = File.ReadAllLines(Path.Join(Application.streamingAssetsPath, "phoneme_dict.txt"));
        for (int i = 0; i < words.Length; i++)
        {
            string s = words[i];
            string[] parts = s.Split();
            if (parts[0] != ";;;") //ignore comments in file
            {
                string key = parts[0];
                dict.Add(key, s.Substring(key.Length + 2));
            }
        }
        // Add codes for punctuation to the dictionary
        dict.Add(",", ",");
        dict.Add(".", ".");
        dict.Add("!", "!");
        dict.Add("?", "?");
        dict.Add("\"", "\"");
        // You could add extra word pronounciations here e.g.
        //dict.Add("somenewword","[phonemes]");
    }

    public string ExpandNumbers(string text)
    {
        return text
            .Replace("0", " ZERO ")
            .Replace("1", " ONE ")
            .Replace("2", " TWO ")
            .Replace("3", " THREE ")
            .Replace("4", " FOUR ")
            .Replace("5", " FIVE ")
            .Replace("6", " SIX ")
            .Replace("7", " SEVEN ")
            .Replace("8", " EIGHT ")
            .Replace("9", " NINE ");
    }

    public string TextToPhonemes(string text)
    {
        string output = "";
        text = ExpandNumbers(text).ToUpper();

        string[] words = text.Split();
        for (int i = 0; i < words.Length; i++)
        {
            output += DecodeWord(words[i]);
        }
        return output;
    }

    //Decode the word into phenomes by looking for the longest word in the dictionary that matches
    //the first part of the word and so on. 
    //This works fairly well but could be improved. The original paper had a model that
    //dealt with guessing the phonemes of words
    public string DecodeWord(string word)
    {
        string output = "";
        int start = 0;
        for (int end = word.Length; end >= 0 && start < word.Length; end--)
        {
            if (end <= start) //no matches
            {
                start++;
                end = word.Length + 1;
                continue;
            }
            string subword = word.Substring(start, end - start);
            if (dict.TryGetValue(subword, out string value))
            {
                output += value + " ";
                start = end;
                end = word.Length + 1;
            }
        }
        return output;
    }

    int[] GetTokens(string ptext)
    {
        string[] p = ptext.Split();
        var tokens = new int[p.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            tokens[i] = Mathf.Max(0, System.Array.IndexOf(phonemes, p[i]));
        }
        return tokens;
    }
    public IEnumerator ExecuteDoInferenceTimeSlicing(string ptext)
    {
        if (m_IsExecuting)
            yield break;

        m_IsExecuting = true;

        int[] tokens = GetTokens(ptext);

        var input = new Tensor<int>(new TensorShape(tokens.Length), tokens);
        var m_Schedule = engine.ScheduleIterable(input);


        float lastYieldTime = Time.realtimeSinceStartup;

        while (m_Schedule.MoveNext())
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastYieldTime > timeThreshold)
            {
                yield return new WaitForEndOfFrame();
                Debug.Log("TextToSpeech : waiting");
                lastYieldTime = currentTime;
            }
        }

        var output = engine.PeekOutput("wav") as Tensor<float>;
        var s = output.ReadbackAndClone();
        var samples = s.DownloadToArray();

        // Debug.Log($"Audio size = {samples.Length / samplerate} seconds");
        if (clip)
            Destroy(clip);
        clip = AudioClip.Create("voice audio", samples.Length, 1, samplerate, false);
        clip.SetData(samples, 0);

        clipLength = clip.length;
        Speak();

        m_IsExecuting = false;

        input.Dispose();
        output.Dispose();
        s.Dispose();

    }

    public async Awaitable DoInferenceASync(string ptext)
    {



        int[] tokens = GetTokens(ptext);

        var input = new Tensor<int>(new TensorShape(tokens.Length), tokens);

        var m_Schedule = engine.ScheduleIterable(input);


        float lastYieldTime = Time.realtimeSinceStartup;


        while (m_Schedule.MoveNext())
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastYieldTime > timeThreshold)
            {
                await Awaitable.EndOfFrameAsync();
                lastYieldTime = currentTime;
                //Debug.Log("TextToSpeech : waiting");
            }
        }
        PeekpMarker.Begin();
        var output = engine.PeekOutput("wav") as Tensor<float>;
        PeekpMarker.End();
        //CloneMarker.Begin();
       // var s = output.ReadbackAndClone();
        var awaitableTensor = await output.ReadbackAndCloneAsync();
        var s = awaitableTensor;
       // CloneMarker.End();
        DownloadMarker.Begin();
        var samples = s.DownloadToArray();
        DownloadMarker.End();
        //  Debug.Log($"Audio size = {samples.Length / samplerate} seconds");
        if (clip)
            Destroy(clip);
        clip = AudioClip.Create("voice audio", samples.Length, 1, samplerate, false);
        clip.SetData(samples, 0);

        clipLength = clip.length;
        Speak();

        awaitableTensor.Dispose();
        input.Dispose();
        output.Dispose();


    }

    public void DoInference(string ptext)
    {
      


        int[] tokens = GetTokens(ptext);

        var input = new Tensor<int>(new TensorShape(tokens.Length), tokens);
        engine.Schedule(input);

        var output = engine.PeekOutput("wav") as Tensor<float>;
        var s = output.ReadbackAndClone();
        var samples = s.DownloadToArray();

        Debug.Log($"Audio size = {samples.Length / samplerate} seconds");
        if (clip)
            Destroy(clip);
        clip = AudioClip.Create("voice audio", samples.Length, 1, samplerate, false);
        clip.SetData(samples, 0);

        clipLength = clip.length;
        Speak();
        input.Dispose();
        output.Dispose();
        s.Dispose();

    }
    private async void Speak()
    {
        
        if (audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();

            await ClearResourses(clip.length) ;
        }
        else
        {
            Debug.Log("There is no audio source");
        }
    }
    public IEnumerator ClearAfterPlay(float time)
    {
        yield return new WaitForSeconds(time + 0.1f);
        Resources.UnloadUnusedAssets();
      //  Debug.Log("ClearAfterPlay");
    }

    public async Awaitable ClearResourses(float time)
    { 
        await Awaitable.WaitForSecondsAsync(time+ 0.1f);
        await Resources.UnloadUnusedAssets();
    }

    private void OnDestroy()
    {
        Clear();
    }
    public void Clear()
    {
   
        if (engine != null)
        {

            engine.Dispose();
            engine = null;
        }


    }
}
