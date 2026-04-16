using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FrameSelector : MonoBehaviour
{
    [Header("UI 연결")]
    public Transform scrollContent;
    public GameObject frameItemPrefab;
    public UnityEngine.UI.Button startButton;
    public UnityEngine.UI.Button editButton;

    private string[] _frameNames = { "frame_01", "frame_02" };
    private int _selectedIndex = 0;
    private List<GameObject> _frameItems = new List<GameObject>();
    private bool _isEditMode = false;

    void Start()
    {
        LoadFrames();
        SelectFrame(0);
        editButton.onClick.AddListener(ToggleEditMode);
    }

    // ── 편집 모드 토글 ──────────────────────────
    private void ToggleEditMode()
    {
        _isEditMode = !_isEditMode;

        // 버튼 텍스트 전환
        editButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text
            = _isEditMode ? "Save" : "Delete Frames";

        // 커스텀 프레임 아이템에만 X 버튼 노출/숨김
        foreach (var item in _frameItems)
        {
            Transform xBtn = item.transform.Find("DeleteButton");
            if (xBtn != null)
                xBtn.gameObject.SetActive(_isEditMode);
        }
    }

    // ── 프레임 로드 ──────────────────────────
    private void LoadFrames()
    {
        // 기존 아이템 전부 제거
        foreach (var item in _frameItems)
            Destroy(item);
        _frameItems.Clear();
        _selectedIndex = 0;

        // 기본 프레임 (X 버튼 없음)
        for (int i = 0; i < _frameNames.Length; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>(
                "Frames/Default/" + _frameNames[i]
            );
            int captured = i;
            CreateFrameItem(tex, captured, isCustom: false, path: null);
        }

        // 커스텀 프레임 (X 버튼 있음)
        var customPaths = CustomFrameHolder.Instance.GetFramePaths();
        for (int i = 0; i < customPaths.Count; i++)
        {
            byte[] fileData = System.IO.File.ReadAllBytes(customPaths[i]);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            int index = _frameNames.Length + i;
            string path = customPaths[i];
            CreateFrameItem(tex, index, isCustom: true, path: path);
        }

        SelectFrame(0);
    }

    private void CreateFrameItem(    
        Texture2D tex, int index, bool isCustom, string path)
    {
        GameObject item = Instantiate(frameItemPrefab, scrollContent);
        _frameItems.Add(item);

        RawImage preview = item.transform.Find("FramePreviewImage")
            .GetComponent<RawImage>();
        preview.texture = tex;

        // 선택 버튼
        int capturedIndex = _frameItems.Count - 1;
        item.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            if (_isEditMode)
            {
                if (isCustom) DeleteCustomFrame(path); // ← 커스텀만 삭제
                return;
            }
            SelectFrame(capturedIndex, isCustom);
            if (isCustom)
                FrameHolder.Instance.SetCustomFrame(path);
        });

        // 커스텀 프레임만 X 버튼 생성
        if (isCustom)
        {
            Transform xBtn = item.transform.Find("DeleteButton");
            if (xBtn != null)
            {
                xBtn.gameObject.SetActive(false); // 기본 숨김
                xBtn.GetComponent<UnityEngine.UI.Button>()
                    .onClick.AddListener(() => DeleteCustomFrame(path));
            }
        }
    }

    // ── 커스텀 프레임 삭제 ──────────────────────────
    private void DeleteCustomFrame(string path)
    {
        CustomFrameHolder.Instance.RemoveFramePath(path);
        LoadFrames(); // 리스트 전체 새로고침

        // 삭제 후 커스텀이 0개면 편집 모드 자동 해제
        if (CustomFrameHolder.Instance.GetFramePaths().Count == 0)
        {
            _isEditMode = false;
            editButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text
                = "Delete Frames";
        }
    }

    private void SelectFrame(int index, bool isCustom = false)
    {
        if (index < 0 || index >= _frameItems.Count) return;

        // 이전 선택 해제
        SetIndicator(_selectedIndex, 0f);
        _selectedIndex = index;
        SetIndicator(_selectedIndex, 1f);

        if (!isCustom && index < _frameNames.Length)
            FrameHolder.Instance.SetFrame(_frameNames[index]);
    }

    private void SetIndicator(int index, float alpha)
    {
        if (index < 0 || index >= _frameItems.Count) return;
        Transform indicator = _frameItems[index].transform.Find("SelectIndicator");
        if (indicator == null) return;

        Image img = indicator.GetComponent<Image>();
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    public void OnFrameEditorButtonPressed() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene("FrameEditorScene");

    public void OnStartButtonPressed() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene("ShootingScene");
}