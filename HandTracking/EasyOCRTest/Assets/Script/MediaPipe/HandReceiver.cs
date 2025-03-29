using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using System.Collections;

public class HandReceiver : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;
    public GameObject pointPrefab_Right;
    public GameObject pointPrefab_Left;
    public GameObject linePrefab;

    public Material leftHandLineMaterial;
    public Material rightHandLineMaterial;

    public TMP_InputField gestureInput;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI gestureText;

    private StringBuilder receivedData = new StringBuilder();

    private readonly int[,] handConnections = new int[,]
    {
        {0, 1}, {1, 2}, {2, 3}, {3, 4},
        {0, 5}, {5, 6}, {6, 7}, {7, 8},
        {0, 9}, {9, 10}, {10, 11}, {11, 12},
        {0, 13}, {13, 14}, {14, 15}, {15, 16},
        {0, 17}, {17, 18}, {18, 19}, {19, 20}
    };

    private ObjectPool<Renderer> pointPool_Left;
    private ObjectPool<Renderer> pointPool_Right;
    private ObjectPool<LineRenderer> linePool_Left;
    private ObjectPool<LineRenderer> linePool_Right;

    private List<Renderer> activePoints_Left = new();
    private List<Renderer> activePoints_Right = new();
    private List<LineRenderer> activeLines_Left = new();
    private List<LineRenderer> activeLines_Right = new();

    private List<List<Landmark>> recordedFrames = new();
    private bool isRecording = false;

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
        gestures = new List<Gesture>
        {
            new Gesture("안녕하세요", IsHello),
            new Gesture("감사합니다", IsThanks),
            new Gesture("사랑해요", IsILoveYou)
        };
    }

    void Start()
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

        pointPool_Left = new ObjectPool<Renderer>(pointPrefab_Left.GetComponent<Renderer>(), 50);
        pointPool_Right = new ObjectPool<Renderer>(pointPrefab_Right.GetComponent<Renderer>(), 50);
        linePool_Left = new ObjectPool<LineRenderer>(linePrefab.GetComponent<LineRenderer>(), 50);
        linePool_Right = new ObjectPool<LineRenderer>(linePrefab.GetComponent<LineRenderer>(), 50);
    }

    public void StartRecording()
    {
        string label = gestureInput.text.Trim();
        if (!string.IsNullOrEmpty(label))
        {

            statusText.text = $"📹 Wait for Recording: {label}";
            StartCoroutine(RecordTime());
        }
    }

    IEnumerator RecordTime()
    {
        //1초 대기
        yield return new WaitForSeconds(recordingWaitTime);

        string label = gestureInput.text.Trim();
        statusText.text = $"📹 Recording gesture: {label} , {recordingTIme}";
        recordedFrames.Clear();
        isRecording = true;
    }

    void Update()
    {
        if(isRecording)
        {
            recordingTIme += Time.deltaTime;
            statusText.text = $"📹 Recording gesture: {gestureInput.text.Trim()} , {recordingTIme}";

            if (recordingTIme >= recordingNeedTime)
            {
                isRecording = false;
                string label = gestureInput.text.Trim();
                string json = JsonUtility.ToJson(new RecordedGesture(label, recordedFrames));
                string folder = Path.Combine(Application.dataPath, "GestureData");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, $"{label}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(path, json);
                statusText.text = $"💾 Saved to: {path}";
            }
        }

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

                            if (isRecording && h == 1)
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

                        if (handData.hands.Count >= 2 && handData.hands[1] != null)
                        {
                            var rightHand = handData.hands[1].landmarks;

                            string detectedGesture = "";
                            foreach (var gesture in gestures)
                            {
                                if (gesture.matchFunc(rightHand))
                                {
                                    detectedGesture = gesture.name;
                                    break;
                                }
                            }

                            if (detectedGesture != lastDetectedGesture)
                            {
                                lastDetectedGesture = detectedGesture;
                                gestureHoldTime = 0f;
                            }
                            else
                            {
                                gestureHoldTime += Time.deltaTime;

                                if (gestureHoldTime >= requiredHoldDuration)
                                    currentGesture = detectedGesture;
                            }

                            gestureText.text = currentGesture;
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

    void ReturnAll()
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

    bool IsThumbExtended(List<Landmark> lm)
    {
        Vector3 v1 = new Vector3(lm[1].x - lm[0].x, lm[1].y - lm[0].y, lm[1].z - lm[0].z);
        Vector3 v2 = new Vector3(lm[4].x - lm[2].x, lm[4].y - lm[2].y, lm[4].z - lm[2].z);
        float angle = Vector3.Angle(v1, v2);
        return angle < 45f || angle > 135f;
    }

    bool IsFingerExtended(List<Landmark> lm, int mcp, int pip, int tip)
    {
        Vector3 v1 = new Vector3(lm[pip].x - lm[mcp].x, lm[pip].y - lm[mcp].y, lm[pip].z - lm[mcp].z);
        Vector3 v2 = new Vector3(lm[tip].x - lm[pip].x, lm[tip].y - lm[pip].y, lm[tip].z - lm[pip].z);
        float angle = Vector3.Angle(v1, v2);
        return angle > 130f;
    }

    bool IsHello(List<Landmark> lm)
    {
        int extended = 0;
        if (IsFingerExtended(lm, 5, 6, 8)) extended++;
        if (IsFingerExtended(lm, 9, 10, 12)) extended++;
        if (IsFingerExtended(lm, 13, 14, 16)) extended++;
        if (IsFingerExtended(lm, 17, 18, 20)) extended++;
        return IsThumbExtended(lm) && extended >= 3;
    }

    bool IsThanks(List<Landmark> lm)
    {
        int folded = 0;
        if (!IsFingerExtended(lm, 5, 6, 8)) folded++;
        if (!IsFingerExtended(lm, 9, 10, 12)) folded++;
        if (!IsFingerExtended(lm, 13, 14, 16)) folded++;
        if (!IsFingerExtended(lm, 17, 18, 20)) folded++;
        return folded >= 4;
    }

    bool IsILoveYou(List<Landmark> lm)
    {
        return IsThumbExtended(lm)
            && IsFingerExtended(lm, 5, 6, 8)
            && !IsFingerExtended(lm, 9, 10, 12)
            && !IsFingerExtended(lm, 13, 14, 16)
            && IsFingerExtended(lm, 17, 18, 20);
    }
}
