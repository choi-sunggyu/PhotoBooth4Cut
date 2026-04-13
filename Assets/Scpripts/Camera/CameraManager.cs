using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CameraManager : MonoBehaviour
{
    [Header("UI 연결")]
    public RawImage cameraFeedImage;
    public RawImage[] slots = new RawImage[4];

    private WebCamTexture _webCamTexture;
    private Texture2D[] _capturedTextures = new Texture2D[4];
    private int _currentSlot = 0;

    void Start()
    {
        StartCamera();
    }

    private void StartCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        string targetCam = "";

        foreach (var device in devices)
        {
            if (device.isFrontFacing)
            {
                targetCam = device.name;
                break;
            }
        }

        _webCamTexture = new WebCamTexture(targetCam, 1080, 1080, 30);
        cameraFeedImage.texture = _webCamTexture;
        _webCamTexture.Play();
    }

    public void OnCaptureButtonPressed()
    {
        if (_currentSlot >= 4) return;
        StartCoroutine(CaptureSlot(_currentSlot));
        _currentSlot++;

        // 4장 완료 시 ResultScene으로 이동
        if (_currentSlot >= 4)
        {
            StartCoroutine(GoToResultScene());
        }
    }

    private System.Collections.IEnumerator GoToResultScene()
    {
        // 캡처 코루틴 완료 대기
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        TextureHolder.Instance.SetTextures(_capturedTextures);
        SceneManager.LoadScene("ResultScene");
    }

    private System.Collections.IEnumerator CaptureSlot(int slotIndex)
    {
        yield return new WaitForEndOfFrame();

        Texture2D snapshot = new Texture2D(
            _webCamTexture.width,
            _webCamTexture.height,
            TextureFormat.RGB24,
            false
        );

        snapshot.SetPixels(_webCamTexture.GetPixels());
        snapshot.Apply();

        _capturedTextures[slotIndex] = snapshot;
        slots[slotIndex].texture = snapshot;
    }

    // 외부에서 촬영된 텍스처 가져갈 때 사용
    public Texture2D[] GetCapturedTextures()
    {
        return _capturedTextures;
    }

    void OnDestroy()
    {
        if (_webCamTexture != null && _webCamTexture.isPlaying)
            _webCamTexture.Stop();
    }
}