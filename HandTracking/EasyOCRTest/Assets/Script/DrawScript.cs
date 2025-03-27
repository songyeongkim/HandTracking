using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class DrawScript : MonoBehaviour
{
    public Texture2D texture;
    public Color drawColor = Color.black;
    public int brushSize = 5;
    public LayerMask drawingLayer;

    private Vector2? lastPixelUV = null; //이전 픽셀 좌표 지정

    [SerializeField]
    private EasyOCRManager easyOCRManager;

    private float _ocrTime;
    private bool _isDrawing = false;
    private bool _isDrawingOver = false;

    private void Start()
    {
        texture = new Texture2D(512, 512);
        GetComponent<Renderer>().material.mainTexture = texture;

        //흰배경 처리
        ClearCanvas();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && IsMouseOverPlane())
        {
            _isDrawing = true;
            _ocrTime = 0;

            Vector2 pixelUV = GetMousePixelPosition();
            if (lastPixelUV == null)
            {
                Draw(pixelUV);
            }
            else
            {
                DrawLine(lastPixelUV.Value, pixelUV);
            }
            lastPixelUV = pixelUV;
        }
        else if (Input.GetMouseButtonUp(0) && _isDrawing)
        {
            lastPixelUV = null;

            //마우스가 떨어졌을 때 텍스쳐 정보 전송
            if (easyOCRManager != null)
            {
                Texture2D processedTexture = PreprocessImage(texture);
                SaveDrawing(processedTexture);

                _isDrawingOver = true;
            }
        }
        else if(_isDrawingOver)
        {
            _ocrTime += Time.deltaTime;

            if(_ocrTime > 1)
            {
                _isDrawingOver = false;
                _isDrawing = false;
                _ocrTime = 0;
                easyOCRManager.StartOCR();
            }
        }
    }

    bool IsMouseOverPlane()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 지정된 Layer의 Plane과 충돌하는지 확인
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, drawingLayer))
        {
            return true;
        }
        return false;
    }

    Vector2 GetMousePixelPosition()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, drawingLayer))
        {
            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x *= texture.width;
            pixelUV.y *= texture.height;
            return pixelUV;
        }

        return Vector2.zero;
    }

    private void Draw(Vector2 pixelUV)
    {
        for (int x = -brushSize; x < brushSize; x++)
        {
            for (int y = -brushSize; y < brushSize; y++)
            {
                float distance = x * x + y * y;
                if (distance < brushSize * brushSize)
                {
                    //float alpha = 1f - (distance / (brushSize * brushSize)); // 점점 부드럽게
                    //Color currentColor = texture.GetPixel((int)pixelUV.x + x, (int)pixelUV.y + y);
                    //Color blendedColor = Color.Lerp(currentColor, drawColor, alpha);

                    texture.SetPixel((int)pixelUV.x + x, (int)pixelUV.y + y, drawColor);
                }
            }
        }
        texture.Apply();
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        int steps = (int)Vector2.Distance(start, end);
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)steps;
            Vector2 interpolatedPos = Vector2.Lerp(start, end, t);
            Draw(interpolatedPos);
        }
    }

    //이진화
    private Texture2D PreprocessImage(Texture2D source)
    {
        int width = source.width;
        int height = source.height;
        Texture2D processed = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                float grayscale = (pixel.r + pixel.g + pixel.b) / 3.0f;
                Color newPixel = grayscale > 0.5f ? Color.white : Color.black; // Otsu Threshold

                // 좌우 반전 + 상하 반전 적용
                int flippedX = width - x - 1;  // 좌우 반전
                int flippedY = height - y - 1; // 상하 반전

                processed.SetPixel(flippedX, flippedY, newPixel);
            }
        }
        processed.Apply();
        return processed;
    }

    private void SaveDrawing(Texture2D source)
    {
        byte[] bytes = source.EncodeToPNG();
        string filePath = Path.Combine(Application.persistentDataPath, "handwriting.png");
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Image saved at: " + filePath);
    }

    public void ClearCanvas()
    {
        // 캔버스를 흰색으로 다시 초기화
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }
        texture.Apply();

        Debug.Log("Canvas cleared!");

    }
}
