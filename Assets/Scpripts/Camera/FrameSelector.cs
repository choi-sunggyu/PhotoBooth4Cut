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

    private string[] _frameNames = { "frame_01", "frame_02" };
    private int _selectedIndex = 0;
    private List<GameObject> _frameItems = new List<GameObject>();

    void Start()
    {
        LoadFrames();
        LoadCustomFrames();
        SelectFrame(0);
    }

    private void LoadFrames()
    {
        for (int i = 0; i < _frameNames.Length; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>(
                "Frames/Default/" + _frameNames[i]
            );
            CreateFrameItem(tex, i, false);
        }

        // 커스텀 프레임 추가
        var customPaths = CustomFrameHolder.Instance.GetFramePaths();
        for (int i = 0; i < customPaths.Count; i++)
        {
            Texture2D tex = NativeGallery.LoadImageAtPath(customPaths[i], 512);
            if (tex == null) continue;

            int index = _frameNames.Length + i;
            CreateFrameItem(tex, index, true);
        }
    }

    private void LoadCustomFrames()
    {
        var customPaths = CustomFrameHolder.Instance.GetFramePaths();
        if (customPaths.Count == 0) return;

        for (int i = 0; i < customPaths.Count; i++)
        {
            byte[] fileData = System.IO.File.ReadAllBytes(customPaths[i]);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            GameObject item = Instantiate(frameItemPrefab, scrollContent);
            _frameItems.Add(item);

            RawImage preview = item.transform.Find("FramePreviewImage")
                .GetComponent<RawImage>();
            preview.texture = tex;

            int index = _frameNames.Length + i;
            string path = customPaths[i];

            item.GetComponent<UnityEngine.UI.Button>()
                .onClick.AddListener(() =>
                {
                    SelectFrame(index, true);
                    // 커스텀 프레임은 경로로 직접 로드
                    FrameHolder.Instance.SetCustomFrame(path);
                });
        }
    }

    private void CreateFrameItem(Texture2D tex, int index, bool isCustom)
    {
        GameObject item = Instantiate(frameItemPrefab, scrollContent);
        _frameItems.Add(item);

        RawImage preview = item.transform.Find("FramePreviewImage")
            .GetComponent<RawImage>();
        preview.texture = tex;

        item.GetComponent<UnityEngine.UI.Button>()
            .onClick.AddListener(() => SelectFrame(index, isCustom));
    }

    private void SelectFrame(int index, bool isCustom = false)
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

        // 기본 프레임 vs 커스텀 프레임
        if (!isCustom && index < _frameNames.Length)
        {
            FrameHolder.Instance.SetFrame(_frameNames[index]);
        }
    }

    public void OnFrameEditorButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("FrameEditorScene");
    }

    public void OnStartButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("ShootingScene");
    }
}