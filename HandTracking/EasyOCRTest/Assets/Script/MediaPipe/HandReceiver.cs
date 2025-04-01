using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Networking;

public class HandReceiver : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;
    public GameObject pointPrefab_Right;
    public GameObject pointPrefab_Left;
    public GameObject linePrefab;

    public Material leftHandLineMaterial;
    public Material rightHandLineMaterial;

    private StringBuilder receivedData = new StringBuilder();

    public readonly int[,] handConnections = new int[,]
    {
        {0, 1}, {1, 2}, {2, 3}, {3, 4},
        {0, 5}, {5, 6}, {6, 7}, {7, 8},
        {0, 9}, {9, 10}, {10, 11}, {11, 12},
        {0, 13}, {13, 14}, {14, 15}, {15, 16},
        {0, 17}, {17, 18}, {18, 19}, {19, 20}
    };

    public ObjectPool<Renderer> pointPool_Left;
    public ObjectPool<Renderer> pointPool_Right;
    public ObjectPool<LineRenderer> linePool_Left;
    public ObjectPool<LineRenderer> linePool_Right;

    public List<Renderer> activePoints_Left = new();
    public List<Renderer> activePoints_Right = new();
    public List<LineRenderer> activeLines_Left = new();
    public List<LineRenderer> activeLines_Right = new();

    public List<List<Landmark>> recordedFrames = new();
    public bool isRecordActivating = false;

    private string currentGesture = "";
    private string lastDetectedGesture = "";
    private float gestureHoldTime = 0f;
    [Range(0.2f, 2f)]
    public float requiredHoldDuration = 0.8f;

    [Range(0.2f, 5f)]
    public float recordingWaitTime = 2f;
    [Range(0.2f, 5f)]
    public float recordingNeedTime = 3f;
    private float recordingTIme = 0f;

    private List<Gesture> gestures;

    private void Awake()
    {
    }

    void Start()
    {
        StartServerConnect();

        pointPool_Left = new ObjectPool<Renderer>(pointPrefab_Left.GetComponent<Renderer>(), 50);
        pointPool_Right = new ObjectPool<Renderer>(pointPrefab_Right.GetComponent<Renderer>(), 50);
        linePool_Left = new ObjectPool<LineRenderer>(linePrefab.GetComponent<LineRenderer>(), 50);
        linePool_Right = new ObjectPool<LineRenderer>(linePrefab.GetComponent<LineRenderer>(), 50);
    }

    public void StartServerConnect()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5050);
            stream = client.GetStream();
            Debug.Log("✅ Connected to Python server");
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Socket connection failed: " + e.Message);
            this.enabled = false;
            return;
        }
    }

    void Update()
    {
        if (stream != null && stream.DataAvailable)
        {
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                receivedData.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                while (receivedData.ToString().Contains("\n"))
                {
                    int index = receivedData.ToString().IndexOf("\n");
                    string jsonString = receivedData.ToString().Substring(0, index).Trim();
                    receivedData.Remove(0, index + 1);

                    try
                    {
                        HandsWrapper handData = JsonUtility.FromJson<HandsWrapper>(jsonString);
                        if (handData == null || handData.hands == null)
                            return;

                        ReturnAll();

                        for (int h = 0; h < handData.hands.Count; h++)
                        {
                            if (handData.hands[h] == null || handData.hands[h].landmarks == null)
                                continue;

                            var landmarks = handData.hands[h].landmarks;

                            for (int i = 0; i < landmarks.Count; i++)
                                landmarks[i].x = 1.0f - landmarks[i].x;

                            if (isRecordActivating && h == 1)
                                recordedFrames.Add(new List<Landmark>(landmarks));

                            var pointPool = (h == 0) ? pointPool_Left : pointPool_Right;
                            var linePool = (h == 0) ? linePool_Left : linePool_Right;
                            var pointList = (h == 0) ? activePoints_Left : activePoints_Right;
                            var lineList = (h == 0) ? activeLines_Left : activeLines_Right;
                            var lineMat = (h == 0) ? leftHandLineMaterial : rightHandLineMaterial;

                            List<Transform> currentHandPoints = new();

                            for (int i = 0; i < landmarks.Count; i++)
                            {
                                float flippedY = 1 - landmarks[i].y;
                                Vector3 pos = new Vector3(
                                    landmarks[i].x * 5f - 2.5f,
                                    flippedY * 5f - 2.5f,
                                    -landmarks[i].z * 5f
                                );

                                if (pointList.Count <= i)
                                    pointList.Add(pointPool.Get());

                                var point = pointList[i];
                                point.transform.position = pos;
                                currentHandPoints.Add(point.transform);
                            }

                            for (int i = 0; i < handConnections.GetLength(0); i++)
                            {
                                int startIdx = handConnections[i, 0];
                                int endIdx = handConnections[i, 1];

                                if (startIdx < currentHandPoints.Count && endIdx < currentHandPoints.Count)
                                {
                                    if (lineList.Count <= i)
                                        lineList.Add(linePool.Get());

                                    var lr = lineList[i];
                                    lr.material = lineMat;
                                    lr.startWidth = 0.03f;
                                    lr.endWidth = 0.03f;
                                    lr.positionCount = 2;
                                    lr.SetPosition(0, currentHandPoints[startIdx].position);
                                    lr.SetPosition(1, currentHandPoints[endIdx].position);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("🧨 JSON Parsing Error: " + e.Message);
                    }
                }
            }
        }
    }

    public void ReturnAll()
    {
        pointPool_Left?.ReturnAll(activePoints_Left);
        pointPool_Right?.ReturnAll(activePoints_Right);
        linePool_Left?.ReturnAll(activeLines_Left);
        linePool_Right?.ReturnAll(activeLines_Right);
    }

    void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
    }
}

[Serializable]
public class Landmark
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class HandLandmarks
{
    public List<Landmark> landmarks;
}

[Serializable]
public class HandsWrapper
{
    public List<HandLandmarks> hands;
}

public class Gesture
{
    public string name;
    public Func<List<Landmark>, bool> matchFunc;

    public Gesture(string name, Func<List<Landmark>, bool> matchFunc)
    {
        this.name = name;
        this.matchFunc = matchFunc;
    }
}

[Serializable]
public class RecordedGesture
{
    public string label;
    public List<FrameData> sequence;

    public RecordedGesture(string label, List<List<Landmark>> frames)
    {
        this.label = label;
        this.sequence = new List<FrameData>();
        foreach (var frame in frames)
        {
            sequence.Add(new FrameData(frame));
        }
    }
}

[Serializable]
public class FrameData
{
    public List<Landmark> landmarks;

    public FrameData(List<Landmark> lm)
    {
        landmarks = lm;
    }
}

[Serializable]
public class PredictionResult
{
    public string gesture;
    public float confidence;
}
