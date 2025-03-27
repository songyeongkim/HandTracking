using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PoseReceiver : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;
    public GameObject pointPrefab; // 점을 나타낼 프리팹
    public Material lineMaterial; // 라인 렌더러용 머터리얼

    private List<GameObject> points = new List<GameObject>(); // 생성된 점들
    private List<LineRenderer> lines = new List<LineRenderer>(); // 연결 선들
    private StringBuilder receivedData = new StringBuilder(); // 버퍼 사용

    void Start()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5050);
            stream = client.GetStream();
            Debug.Log("Connected to Python Server");
        }
        catch (Exception e)
        {
            Debug.LogError("Socket Error: " + e.Message);
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
                        // JSON 데이터 파싱
                        PoseData poseData = JsonUtility.FromJson<PoseData>(jsonString);

                        if (poseData == null || poseData.landmarks == null || poseData.connections == null)
                        {
                            Debug.LogError("JSON Parsing Error: Missing data");
                            return;
                        }

                        // 기존 점 삭제 후 다시 생성
                        foreach (var point in points)
                            Destroy(point);
                        points.Clear();

                        foreach (var landmark in poseData.landmarks)
                        {
                            float flippedY = 1 - landmark.y; // y값 반전
                            Vector3 position = new Vector3(landmark.x * 5, flippedY * 5, landmark.z * 5);
                            GameObject newPoint = Instantiate(pointPrefab, position, Quaternion.identity);
                            points.Add(newPoint);
                        }

                        // 기존 선 삭제 후 다시 생성
                        foreach (var line in lines)
                            Destroy(line.gameObject);
                        lines.Clear();

                        foreach (string connection in poseData.connections)
                        {
                            string[] indices = connection.Split(',');
                            int startIdx = int.Parse(indices[0]);
                            int endIdx = int.Parse(indices[1]);

                            if (startIdx < points.Count && endIdx < points.Count)
                            {
                                GameObject lineObject = new GameObject("Line");
                                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                                lineRenderer.material = lineMaterial;
                                lineRenderer.startWidth = 0.05f;
                                lineRenderer.endWidth = 0.05f;
                                lineRenderer.positionCount = 2;
                                lineRenderer.SetPosition(0, points[startIdx].transform.position);
                                lineRenderer.SetPosition(1, points[endIdx].transform.position);
                                lines.Add(lineRenderer);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("JSON Parsing Error: " + e.Message);
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        stream.Close();
        client.Close();
    }

    [Serializable]
    public class Landmark
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class PoseData
    {
        public List<Landmark> landmarks;
        public List<string> connections;  // **List<int[]> → List<string> 사용**
    }
}
