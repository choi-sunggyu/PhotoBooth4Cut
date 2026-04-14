using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FrameSelector : MonoBehaviour
{
    [Header("UI 연결")]
    public Transform scrollContent;
    public GameObject frameItemPrefab;
    public UnityEngine.UI.Button startButton;

    private string[] _frameNames = { "frame_01", "frame_02" };
    private int _selectedIndex = 0;
    private GameObject[] _frameItems;

    void Start()
    {
        _frameItems = new GameObject[_frameNames.Length];
        LoadFrames();
        SelectFrame(0); // 기본 첫 번째 선택
    }

    private void LoadFrames()
    {
        for (int i = 0; i < _frameNames.Length; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>("Frames/Default/" + _frameNames[i]);

            GameObject item = Instantiate(frameItemPrefab, scrollContent);
            _frameItems[i] = item;

            // 미리보기 이미지 설정
            RawImage preview = item.transform.Find("FramePreviewImage").GetComponent<RawImage>();
            preview.texture = tex;

            // 버튼 클릭 이벤트
            int index = i;
            item.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectFrame(index));
        }
    }

    private void SelectFrame(int index)
    {
        // 이전 선택 해제
        if (_frameItems[_selectedIndex] != null)
        {
            Image indicator = _frameItems[_selectedIndex]
                .transform.Find("SelectIndicator")
                .GetComponent<Image>();
            Color c = indicator.color;
            c.a = 0f;
            indicator.color = c;
        }

        _selectedIndex = index;

        // 새 선택 표시
        Image newIndicator = _frameItems[_selectedIndex]
            .transform.Find("SelectIndicator")
            .GetComponent<Image>();
        Color nc = newIndicator.color;
        nc.a = 1f;
        newIndicator.color = nc;

        // FrameHolder에 선택된 프레임 저장
        FrameHolder.Instance.SetFrame(_frameNames[index]);
    }

    public void OnStartButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("ShootingScene");
    }
}