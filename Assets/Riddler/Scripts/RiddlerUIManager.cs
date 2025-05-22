using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RiddlerUIManager : Singleton<RiddlerUIManager>
{
    public UIDocument document;
    private VisualElement root;
    private VisualElement AnswerDlg;
    private Button AnswerRiddleButton;
    private Button RequestRiddleButton;
    private Button NextButton;
    private Button PrevButton;
    private Button ResetButton;
    private Button RecordButton;

    private Label RiddlerQuestion;
    private Label RiddleAnswer;
    private DropdownField MicrophoneDropdown;

    public bool isRecording = false;
    public bool isShowingRecordButton = false;
    private List<string> mics = new List<string>();
    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        root = document.rootVisualElement;

        AnswerRiddleButton = root.Q<Button>("AnswerRiddleButton");
        RequestRiddleButton = root.Q<Button>("RequestRiddleButton");
        RiddlerQuestion = root.Q<Label>("RiddlerQuestion");
        RiddleAnswer = root.Q<Label>("RiddleAnswer");

        AnswerRiddleButton.RegisterCallback<ClickEvent>(ev => OnAnswerRiddleButtonClicked(ev));
        RequestRiddleButton.RegisterCallback<ClickEvent>(ev => OnRequestRiddleButtonClicked(ev));

        NextButton = root.Q<Button>("NextButton");
        PrevButton = root.Q<Button>("PrevButton");
        ResetButton = root.Q<Button>("ResetButton");

        RecordButton = root.Q<Button>("RecordButton");
        AnswerDlg = root.Q<VisualElement>("AnswerDlg");

        MicrophoneDropdown =  root.Q<DropdownField>("MicrophoneDropdown");

        NextButton.RegisterCallback<ClickEvent>(ev => OnNextRiddleButtonClicked(ev));
        ResetButton.RegisterCallback<ClickEvent>(ev => OnResetRiddlesButtonClicked(ev));
        PrevButton.RegisterCallback<ClickEvent>(ev => OnPrevRiddleButtonClicked(ev));
        RecordButton.RegisterCallback<ClickEvent>(ev => OnRecordButtonClicked(ev));


        foreach (var device in Microphone.devices)
        {
            mics.Add(device);
        }
        MicrophoneDropdown.choices = mics;
        MicrophoneDropdown.RegisterCallback<ChangeEvent<string>>(evt => OnMicChanged(evt));


        
        HideRecordingDlg();

    }
    private void OnMicChanged(ChangeEvent<string> evt)
    {

        RiddlerManager.Instance.SetMic(evt.newValue);
    }
    private void OnRecordButtonClicked(ClickEvent ev)
    {
        isRecording =  !isRecording;
        RiddlerManager.Instance.RecordAudio(isRecording);
        if (isRecording)
        {
            RecordButton.text = "Stop n Submit";
        }
        else
        {
            HideRecordingDlg();

        }
    }
    private void OnNextRiddleButtonClicked(ClickEvent ev)
    {
        RiddlerManager.Instance.NextRiddle();
    }
    private void OnResetRiddlesButtonClicked(ClickEvent ev)
    {
        RiddlerManager.Instance.ResetRiddles();
    }
    private void OnPrevRiddleButtonClicked(ClickEvent ev)
    {
        RiddlerManager.Instance.PrevRiddle();
    }
    private void OnAnswerRiddleButtonClicked(ClickEvent ev)
    {

        isShowingRecordButton = !isShowingRecordButton;
        if (isShowingRecordButton)
        {
            ShowRecordingDlg();
        }
        else
        {
            HideRecordingDlg();
        }

    }
    public void HideRecordingDlg()
    {
        AnswerDlg.style.display = DisplayStyle.None;
        isRecording = false;
        isShowingRecordButton = false;
    }
    public void ShowRecordingDlg()
    {
        RecordButton.text = "Record";
        AnswerDlg.style.display = DisplayStyle.Flex;
       // isShowingRecordButton = true;
    }
    private void OnRequestRiddleButtonClicked(ClickEvent ev)
    {
        RiddlerManager.Instance.RequestRiddle();
    }
    public void SetRiddle(string s)
    {
        if(RiddlerQuestion != null)
            RiddlerQuestion.text = s;
    }
    public void SetAnswer(string s)
    {
        if (RiddleAnswer != null)
            RiddleAnswer.text = s;
    }
    public void CheckAnswer(string s)
    {
        Debug.Log(s);
        SetAnswer(s);
    }
}
