using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class HandReplayer : MonoBehaviour
{
    public HandReceiver receiver; // 기존 HandReceiver를 연결해주세요
    public PosBoneViewer posBoneViewer;
    public TextAsset jsonFile;    // Inspector에서 JSON 지정 가능
    public float frameDelay = 0.03f;

    [System.Serializable]
    public class FrameData
    {
        public List<Landmark> landmarks;
    }

    [System.Serializable]
    public class RecordedGesture
    {
        public string label;
        public List<FrameData> sequence;
    }

    public void StartReplay()
    {
        if (jsonFile != null)
        {
            var gesture = JsonUtility.FromJson<RecordedGesture>(jsonFile.text);
            StartCoroutine(ReplayGesture(gesture.sequence));
        }
        else
        {
            Debug.LogWarning("? JSON 파일이 비어있습니다.");
        }
    }

    IEnumerator ReplayGesture(List<FrameData> frames)
    {
        if (receiver.returnAllAction != null)
            receiver.returnAllAction.Invoke();

        for (int f = 0; f < frames.Count; f++)
        {
            if (receiver.returnAllAction != null)
                receiver.returnAllAction.Invoke(); // 기존 오브젝트 초기화

            var landmarks = frames[f].landmarks;
            List<Transform> handPoints = new();

            for (int i = 0; i < landmarks.Count; i++)
            {
                float x = landmarks[i].x;
                float y = 1 - landmarks[i].y;
                float z = -landmarks[i].z;

                Vector3 pos = new Vector3(x * 5f - 2.5f, y * 5f - 2.5f, z * 5f);

                if (posBoneViewer.activePoints_Right.Count <= i)
                    posBoneViewer.activePoints_Right.Add(posBoneViewer.pointPool_Right.Get());

                var point = posBoneViewer.activePoints_Right[i];
                point.transform.position = pos;
                handPoints.Add(point.transform);
            }

            for (int i = 0; i < receiver.handConnections.GetLength(0); i++)
            {
                int start = receiver.handConnections[i, 0];
                int end = receiver.handConnections[i, 1];

                if (start < handPoints.Count && end < handPoints.Count)
                {
                    if (posBoneViewer.activeLines_Right.Count <= i)
                        posBoneViewer.activeLines_Right.Add(posBoneViewer.linePool_Right.Get());

                    var line = posBoneViewer.activeLines_Right[i];
                    line.material = posBoneViewer.rightHandLineMaterial;
                    line.startWidth = 0.03f;
                    line.endWidth = 0.03f;
                    line.positionCount = 2;
                    line.SetPosition(0, handPoints[start].position);
                    line.SetPosition(1, handPoints[end].position);
                }
            }

            yield return new WaitForSeconds(frameDelay);
        }
    }
}
