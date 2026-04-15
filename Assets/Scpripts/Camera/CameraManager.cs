using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CameraManager : MonoBehaviour
{
    [Header("UI 연결")]
    public RawImage cameraFeedImage;
    public TextMeshProUGUI counterText;

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

        // 연결된 카메라 목록 확인
        foreach (var device in devices)
        {
            Debug.Log("감지된 카메라 : " + device.name + " / 전면 : " + device.isFrontFacing);
        }

        if (devices.Length == 0)
        {
            Debug.LogError("연결된 카메라 없음");
            return;
        }

        // PC : 전면 카메라 없으면 첫 번째 카메라 사용
        // iPad : 전면 카메라 우선 사용
        string targetCam = devices[0].name;
        foreach (var device in devices)
        {
            if (device.isFrontFacing)
            {
                targetCam = device.name;
                break;
            }
        }

        Debug.Log("선택된 카메라 : " + targetCam);

        _webCamTexture = new WebCamTexture(
            targetCam,
            Screen.width,    // 기기 최대 해상도 사용
            Screen.height,
            30
        );
        cameraFeedImage.texture = _webCamTexture;
        _webCamTexture.Play();

        StartCoroutine(WaitForCameraReady());
    }

    private System.Collections.IEnumerator WaitForCameraReady()
    {
        float timeout = 5f; // 5초 대기
        float elapsed = 0f;

        // 웹캠 width가 0이면 아직 준비 안 된 것
        while (_webCamTexture.width <= 16)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= timeout)
            {
                Debug.LogError("카메라 초기화 타임아웃 - width : " + _webCamTexture.width);
                yield break;
            }

            yield return null;
        }
        Debug.Log("카메라 준비 완료 : " + _webCamTexture.width + "x" + _webCamTexture.height);

        _webCamTexture.filterMode = FilterMode.Bilinear;
        // 준비 완료 후 texture 재연결
        cameraFeedImage.texture = _webCamTexture;
    }

    public void OnCaptureButtonPressed()
    {
        if (_currentSlot >= 4) return;
        StartCoroutine(CaptureSlot(_currentSlot));
        _currentSlot++;

        if (counterText != null)
            counterText.text = _currentSlot + " / 4";

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

        // 웹캠 실행 여부 체크 추가
        if (!_webCamTexture.isPlaying || _webCamTexture.width <= 16)
        {
            Debug.LogWarning("카메라 아직 준비 안 됨");
            _currentSlot--; // 슬롯 인덱스 되돌리기
            yield break;
        }

        Texture2D snapshot = new Texture2D(
            _webCamTexture.width,
            _webCamTexture.height,
            TextureFormat.RGBA32,
            false
        );
        snapshot.filterMode = FilterMode.Bilinear;
        snapshot.SetPixels(_webCamTexture.GetPixels());
        snapshot.Apply();

        _capturedTextures[slotIndex] = snapshot;
    }

    // 외부에서 촬영된 텍스처 가져갈 때 사용
    public Texture2D[] GetCapturedTextures()
    {
        return _capturedTextures;
    }

    void OnApplicationPause(bool paused)
    {
        if (_webCamTexture == null) return;

        if (paused)
            _webCamTexture.Stop();
        else
            _webCamTexture.Play();
    }

    void OnDestroy()
    {
        if (_webCamTexture != null)
        {
            _webCamTexture.Stop();
            _webCamTexture = null;
        }
    }
}