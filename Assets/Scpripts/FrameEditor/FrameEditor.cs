using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;

public class FrameEditor : MonoBehaviour
{
    [Header("에디터 영역")]
    public Image backgroundLayer;
    public Transform contentLayer;
    public Image frameHoleLayer;

    [Header("SubPanel")]
    public GameObject colorPicker;
    public GameObject stickerGrid;
    public GameObject textInputPanel;
    public TMP_InputField textInputField;

    [Header("프리팹")]
    public GameObject draggableElementPrefab;

    [Header("스티커 그리드")]
    public Transform stickerGridContent;

    private Color _bgColor = Color.white;

    // 기본 제공 색상 팔레트
    private Color[] _palette = new Color[]
    {
        Color.white,
        Color.black,
        new Color(1f,    0.4f,  0.4f),   // 빨강
        new Color(1f,    0.8f,  0.4f),   // 노랑
        new Color(0.4f,  0.8f,  0.4f),   // 초록
        new Color(0.4f,  0.7f,  1f),     // 파랑
        new Color(0.7f,  0.4f,  1f),     // 보라
        new Color(1f,    0.6f,  0.8f),   // 핑크
        new Color(0.6f,  0.9f,  0.9f),   // 민트
        new Color(1f,    0.8f,  0.6f),   // 살구
        new Color(0.5f,  0.5f,  0.5f),   // 회색
        new Color(0.9f,  0.85f, 0.7f),   // 베이지
    };

    void Start()
    {
        ShowSubPanel("");
        LoadColorPalette();
        LoadStickers();
    }

    // ── SubPanel 전환 ──────────────────────
    public void ShowSubPanel(string panel)
    {
        colorPicker.SetActive(panel == "color");
        stickerGrid.SetActive(panel == "sticker");
        textInputPanel.SetActive(panel == "text");
    }

    // ── 배경색 팔레트 생성 ──────────────────
    private void LoadColorPalette()
    {
        Transform grid = colorPicker.transform;

        foreach (Transform child in grid)
            Destroy(child.gameObject);

        foreach (var col in _palette)
        {
            GameObject btn = new GameObject("ColorBtn");
            btn.transform.SetParent(grid);

            RectTransform rt = btn.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;

            Image img = btn.AddComponent<Image>();
            img.color = col;

            Button button = btn.AddComponent<Button>();
            Color captured = col;
            button.onClick.AddListener(() => SetBackgroundColor(captured));
        }
    }

    // ── 스티커 로드 ──────────────────────
    private void LoadStickers()
    {
        foreach (Transform child in stickerGridContent)
            Destroy(child.gameObject);

        Texture2D[] stickers = Resources.LoadAll<Texture2D>("DefaultStickers");

        foreach (var sticker in stickers)
        {
            GameObject btn = new GameObject(sticker.name);
            btn.transform.SetParent(stickerGridContent);

            RectTransform rt = btn.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;

            RawImage img = btn.AddComponent<RawImage>();
            img.texture = sticker;

            Button button = btn.AddComponent<Button>();
            Texture2D captured = sticker;
            button.onClick.AddListener(() => SpawnSticker(captured));
        }
    }

    // ── 배경색 설정 ──────────────────────
    private void SetBackgroundColor(Color color)
    {
        _bgColor = color;
        backgroundLayer.color = color;
    }

    // ── 이미지 불러오기 ───────────────────
    public void OnImportImageButtonPressed()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path == null) return;

            Texture2D tex = NativeGallery.LoadImageAtPath(path, 1024);
            if (tex == null) return;

            SpawnImage(tex);
        }, "이미지 선택");
    }

    // ── 이미지/스티커 배치 ─────────────────
    private void SpawnImage(Texture2D tex)
    {
        GameObject element = Instantiate(
            draggableElementPrefab, contentLayer
        );
        RawImage img = element.GetComponent<RawImage>();
        if (img == null) img = element.AddComponent<RawImage>();
        img.texture = tex;
        element.GetComponent<RectTransform>().localPosition = Vector3.zero;
    }

    private void SpawnSticker(Texture2D tex)
    {
        GameObject element = Instantiate(
            draggableElementPrefab, contentLayer
        );
        RawImage img = element.GetComponent<RawImage>();
        if (img == null) img = element.AddComponent<RawImage>();
        img.texture = tex;

        RectTransform rt = element.GetComponent<RectTransform>();
        rt.localPosition = Vector3.zero;
        rt.sizeDelta = new Vector2(150, 150);
    }

    // ── 텍스트 추가 ──────────────────────
    public void OnAddTextButtonPressed()
    {
        if (string.IsNullOrEmpty(textInputField.text)) return;

        GameObject textObj = new GameObject("TextElement");
        textObj.transform.SetParent(contentLayer);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = textInputField.text;
        tmp.fontSize  = 80;
        tmp.color     = Color.black;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(500, 150);
        rt.localPosition = Vector3.zero;
        rt.localScale    = Vector3.one;

        textObj.AddComponent<DraggableElement>();
        textInputField.text = "";
        ShowSubPanel("");
    }

    // ── 저장 ─────────────────────────────
    public void OnSaveButtonPressed()
    {
        StartCoroutine(SaveFrame());
    }

    private System.Collections.IEnumerator SaveFrame()
    {
        yield return new WaitForEndOfFrame();

        // EditorArea를 RenderTexture로 캡처
        RenderTexture rt = new RenderTexture(1200, 1800, 24);
        Camera cam = Camera.main;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D saved = new Texture2D(1200, 1800, TextureFormat.RGBA32, false);
        saved.ReadPixels(new Rect(0, 0, 1200, 1800), 0, 0);
        saved.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 커스텀 프레임으로 저장
        byte[] png = saved.EncodeToPNG();
        string fileName = "custom_frame_" + System.DateTime.Now.Ticks + ".png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, png);

        // CustomFrameHolder에 등록
        CustomFrameHolder.Instance.AddFrame(path);

        Debug.Log("프레임 저장 완료 : " + path);
        SceneManager.LoadScene("MainScene");
    }

    public void OnBackButtonPressed()
    {
        SceneManager.LoadScene("MainScene");
    }
}