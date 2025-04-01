using UnityEngine;
using System.Collections;
using TMPro;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

public class HandPosPredictor : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private TextMeshProUGUI gestureText;

    [SerializeField]
    private HandReceiver _handReceiver;


    private List<List<Landmark>> recordedFrames;

    public Action<String> GestureReturnAction;

    private void Awake()
    {
        if (_handReceiver != null)
            recordedFrames = _handReceiver.recordedFrames;
    }

    public void StartPredicting()
    {
        statusText.text = $"📹 Wait for Predicting";
        StartCoroutine(PredictGestureFromModel((gestureString) =>
        {
            Debug.Log(gestureString);

            if (GestureReturnAction != null)
                GestureReturnAction.Invoke(gestureString);

        }));
    }

    IEnumerator PredictGestureFromModel(Action<string> callbackText)
    {
        gestureText.text = null;
        statusText.text = "⏳ Get ready...";
        yield return new WaitForSeconds(1f); // 1초 대기

        _handReceiver.isRecordActivating = true;
        recordedFrames.Clear();
        statusText.text = "Predicting gesture";

        yield return new WaitForSeconds(5f); // 5초 녹화


        while (recordedFrames.Count < 150)
        {
            recordedFrames.Add(new List<Landmark>(new Landmark[21]));
        }

        _handReceiver.isRecordActivating = false;

        var wrapper = new RecordedGesture("predict", recordedFrames);
        string json = JsonUtility.ToJson(wrapper);

        using var request = new UnityEngine.Networking.UnityWebRequest("http://127.0.0.1:8000/predict", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("📩 받은 JSON 원문: " + responseJson);
            var response = JsonUtility.FromJson<PredictionResult>(responseJson);
            gestureText.text = $"🤖 {response.gesture}";

            callbackText.Invoke(response.gesture);
        }
        else
        {
            Debug.LogError("🔥 Predict request failed: " + request.error);
        }
    }
}
