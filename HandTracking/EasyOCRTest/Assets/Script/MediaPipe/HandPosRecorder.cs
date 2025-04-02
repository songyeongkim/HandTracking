using UnityEngine;
using System.Collections;
using TMPro;
using System.IO;
using System;
using System.Collections.Generic;

public class HandPosRecorder : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private TMP_InputField gestureInput;

    [SerializeField]
    private HandReceiver _handReceiver;


    private List<List<Landmark>> recordedFrames;

    private int _recordNum = 1;

    private void Awake()
    {
        if (_handReceiver != null)
            recordedFrames = _handReceiver.recordedFrames;
    }

    public void StartRecording()
    {
        string label = gestureInput.text.Trim();
        if (!string.IsNullOrEmpty(label))
        {
            StartCoroutine(RecordGesture(label));
        }
    }

    public void StopRecording()
    {
        StopAllCoroutines();
        statusText.text = "Stopped";
    }


    IEnumerator RecordGesture(string label)
    {
        while(_recordNum < 10)
        {
            statusText.text = "⏳ Get ready..." + _recordNum + " / 10";
            yield return new WaitForSeconds(1f); // 1초 대기

            _handReceiver.isRecordActivating = true;
            recordedFrames.Clear();
            statusText.text = $"📹 Recording gesture: {label}";

            yield return new WaitForSeconds(3f); // 5초간 녹화

            while (recordedFrames.Count < 90)
            {
                recordedFrames.Add(new List<Landmark>(new Landmark[21]));
            }

            _handReceiver.isRecordActivating = false;
            string json = JsonUtility.ToJson(new RecordedGesture(label, recordedFrames));
            string folder = Path.Combine(Application.dataPath, "GestureData");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"{label}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, json);
            statusText.text = $"💾 Saved to: {path}";

            yield return new WaitForSeconds(1f); // 1초 대기
            _recordNum++;
        }

        statusText.text = "Recording Completed";
        _recordNum = 0;

    }
}
