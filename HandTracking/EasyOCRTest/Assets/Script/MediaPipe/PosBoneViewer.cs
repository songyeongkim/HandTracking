using System.Collections.Generic;
using UnityEngine;

public class PosBoneViewer : MonoBehaviour
{
    [SerializeField]
    public GameObject pointPrefab_Right;

    [SerializeField]
    public GameObject pointPrefab_Left;

    [SerializeField]
    public GameObject linePrefab;

    public Material leftHandLineMaterial;
    public Material rightHandLineMaterial;

    public ObjectPool<Renderer> pointPool_Left;
    public ObjectPool<Renderer> pointPool_Right;
    public ObjectPool<LineRenderer> linePool_Left;
    public ObjectPool<LineRenderer> linePool_Right;

    public List<Renderer> activePoints_Left = new();
    public List<Renderer> activePoints_Right = new();
    public List<LineRenderer> activeLines_Left = new();
    public List<LineRenderer> activeLines_Right = new();

    [SerializeField]
    private HandReceiver _handReceiver;

    private int[,] handConnections;


    private void Start()
    {
        pointPool_Left = new ObjectPool<Renderer>(pointPrefab_Left.GetComponent<Renderer>(), 50);
        pointPool_Right = new ObjectPool<Renderer>(pointPrefab_Right.GetComponent<Renderer>(), 50);
        linePool_Left = new ObjectPool<LineRenderer>(linePrefab.GetComponent<LineRenderer>(), 50);
        linePool_Right = new ObjectPool<LineRenderer>(linePrefab.GetComponent<LineRenderer>(), 50);

        if(_handReceiver != null)
        {
            _handReceiver.returnAllAction += ReturnAll;
            _handReceiver.handViewerAction += DrawPoints;
            handConnections = _handReceiver.handConnections;
        }
    }

    public void ReturnAll()
    {
        pointPool_Left?.ReturnAll(activePoints_Left);
        pointPool_Right?.ReturnAll(activePoints_Right);
        linePool_Left?.ReturnAll(activeLines_Left);
        linePool_Right?.ReturnAll(activeLines_Right);
    }

    public void DrawPoints(int handDir, HandsWrapper handData)
    {
        var landmarks = handData.hands[handDir].landmarks;

        var pointPool = (handDir == 0) ? pointPool_Left : pointPool_Right;
        var linePool = (handDir == 0) ? linePool_Left : linePool_Right;
        var pointList = (handDir == 0) ? activePoints_Left : activePoints_Right;
        var lineList = (handDir == 0) ? activeLines_Left : activeLines_Right;
        var lineMat = (handDir == 0) ? leftHandLineMaterial : rightHandLineMaterial;

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
