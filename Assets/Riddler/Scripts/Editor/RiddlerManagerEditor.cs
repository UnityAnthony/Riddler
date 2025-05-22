using Codice.Client.GameUI.Update;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RiddlerManager))]
public class RiddlerManagerEditor : Editor
{
    VisualElement root;
    VisualElement defaultElement;

    Button RecordButton;
    public override VisualElement CreateInspectorGUI()
    {
        root = new VisualElement();
        defaultElement = new VisualElement();
        InspectorElement.FillDefaultInspector(defaultElement, this.serializedObject, this);
        PopulateInspector(root);
        root.TrackSerializedObjectValue(serializedObject, (obj) => UpdateUI(obj));
        return root;
    }

    private void UpdateUI(SerializedObject obj)
    {
        PopulateInspector(root);
    }

    private void PopulateInspector(VisualElement root)
    {
        root.Clear();
        root.Add(defaultElement);
        //current riddle
        RiddlerManager rM = (RiddlerManager)target;

        VisualElement QuestionAnswerHolder = new VisualElement();
        VisualElement IndexHolder = new VisualElement();
        VisualElement RecordingPanel = new VisualElement();     
        
        #region QuestionAnswerHolder
        Button RequestRiddleButton = new Button();
        RequestRiddleButton.text = "Request";
        Button AnswerRiddleButton = new Button();
        AnswerRiddleButton.text = "Answer";

        RequestRiddleButton.RegisterCallback<ClickEvent>(ev => OnRequestRiddleButtonClicked(ev));
        AnswerRiddleButton.RegisterCallback<ClickEvent>(ev => OnAnswerRiddleButtonClicked(ev));

        QuestionAnswerHolder.Add(RequestRiddleButton);
        QuestionAnswerHolder.Add(AnswerRiddleButton);
        #endregion

        #region IndexHolder

        Button IncIndexButton = new Button();
        IncIndexButton.text = "Incease";
        Button DecIndexButton = new Button();
        DecIndexButton.text = "Decrease";
        Button ResetIndexButton = new Button();
        ResetIndexButton.text = "Reset";

        IncIndexButton.RegisterCallback<ClickEvent>(ev => OnIncIndexButtonClicked(ev));
        DecIndexButton.RegisterCallback<ClickEvent>(ev => OnDecIndexButtonClicked(ev));
        ResetIndexButton.RegisterCallback<ClickEvent>(ev => OnResetIndexButtonClicked(ev));

        IndexHolder.Add(IncIndexButton);
        IndexHolder.Add(DecIndexButton);
        IndexHolder.Add(ResetIndexButton);

        #endregion

        #region RecordingPanel
        RecordButton = new Button();
        if(!rM.GetIsRecording())
            RecordButton.text = "Record";
        else
            RecordButton.text = "Save";
        Button PlayButton = new Button();
        PlayButton.text = "Play";
        Button StopButton = new Button();
        StopButton.text = "Stop";
        Button PauseButton = new Button();
        PauseButton.text = "Pause";

        RecordButton.RegisterCallback<ClickEvent>(ev => OnRecordButtonClicked(ev));
        PlayButton.RegisterCallback<ClickEvent>(ev => OnPlayButtonClicked(ev));
        StopButton.RegisterCallback<ClickEvent>(ev => OnStopButtonClicked(ev));
        PauseButton.RegisterCallback<ClickEvent>(ev => OnPauseButtonClicked(ev));


        RecordingPanel.Add(RecordButton);
        RecordingPanel.Add(PlayButton);
        RecordingPanel.Add(PauseButton);
        RecordingPanel.Add(StopButton);
        #endregion

        #region Add Style and VEs To Root
        QuestionAnswerHolder.style.flexDirection = FlexDirection.Row;
        IndexHolder.style.flexDirection = FlexDirection.Row;
        RecordingPanel.style.flexDirection = FlexDirection.Row;

        root.Add(IndexHolder);
        root.Add(QuestionAnswerHolder);
        root.Add(RecordingPanel);
        #endregion

        #region Current Riddle
        if (rM.riddles.Count > 0)
        {
            if (rM.riddles[rM.currentRiddle])
            {
                Foldout foldout = new Foldout();
                foldout.text = "Riddle: " + rM.riddles[rM.currentRiddle].name;
                var riddelScriptableObj = new SerializedObject(rM.riddles[rM.currentRiddle]);
                // Create InspectorElement to display ScriptableObject properties
                var inspectorElement = new InspectorElement(riddelScriptableObj);
                foldout.Add(inspectorElement);
                root.Add(foldout);
            }
        }
        #endregion

    }
    #region Record


    private void OnRecordButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;

        if (rM.GetIsRecording())
        {
            RecordButton.text = "Save";
            rM.StopRecording();
            rM.SaveRecording();
        }
        else
        {
            RecordButton.text = "Record";
            rM.StartRecording();
           
        }
        PopulateInspector(root);

    }
    private void OnPlayButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.PlayRecording();

    }
    private void OnStopButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.StopReplay();

    }
    private void OnPauseButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.PauseReplay();

    }
    #endregion
    #region Inc/Dec/Reset
    private void OnIncIndexButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.NextRiddle();
        PopulateInspector(root);
    }
    private void OnDecIndexButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.PrevRiddle();
        PopulateInspector(root);
    }
    private void OnResetIndexButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.ResetRiddles();
        PopulateInspector(root);
    }
#endregion
   
    #region Answer/Request

    private void OnAnswerRiddleButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.AnswerRiddle();
    }
    private void OnRequestRiddleButtonClicked(ClickEvent ev)
    {
        RiddlerManager rM = (RiddlerManager)target;
        rM.RequestRiddle();
    }
    #endregion
}
