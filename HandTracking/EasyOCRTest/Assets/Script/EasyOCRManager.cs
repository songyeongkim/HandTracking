using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.IO;
using TMPro;

public class EasyOCRManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;  // UI TextMeshPro ����
    public string pythonPath = @"C:\Users\redjack11\AppData\Local\Programs\Python\Python313\python.exe";  // Python ���� ���� ���
    public string scriptPath = @"C:\Users\redjack11\Desktop\OCRTest\EasyOCR\ocr_script.py"; // OCR ��ũ��Ʈ ���

    void Start()
    {
        
    }

    public void StartOCR()
    {
        StartCoroutine(RunOCR());
    }

    IEnumerator RunOCR()
    {
        string imagePath = Path.Combine(Application.persistentDataPath, "handwriting.png");
        string result = RunPythonOCR(imagePath);
        resultText.text = "OCR ���: " + result;  // UI�� ��� ǥ��
        yield return null;
    }

    string RunPythonOCR(string imagePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = pythonPath;
        startInfo.Arguments = $"\"{scriptPath}\" \"{imagePath}\"";
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        process.Close();

        return output;
    }
}