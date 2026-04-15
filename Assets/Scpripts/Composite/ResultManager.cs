using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    [Header("이미지")]
    public RawImage compositeImage;
    public Transform decorationLayer;

    [Header("탭")]
    public GameObject frameContent;
    public GameObject filterContent;
    public GameObject stickerContent;

    [Header("프레임")]
    public Transform frameGrid;      // FrameGrid 연결
    public GameObject frameItemPrefab;

    [Header("필터")]
    public Transform filterGrid;     // FilterGrid 연결

    [Header("스티커")]
    public Transform stickerGrid;    // StickerGrid 연결
    public GameObject draggableElementPrefab;

    [Header("버튼")]
    public Button saveButton;
    public Button shareButton;

    // 프레임/슬롯 상수 (여러 메서드에서 공유)
    private const int FrameW  = 1200, FrameH  = 1800;
    private const int SlotW   = 550,  SlotH   = 715;
    private static readonly int[] SlotX = { 40,  610, 40,  610 };
    private static readonly int[] SlotY = { 1035, 1035, 300, 300 };

    private Texture2D _finalTexture;
    private Texture2D _baseTexture;
    private string[] _frameNames = { "frame_01", "frame_02" };
    private int _selectedFrameIndex = 0;

    private Texture2D _photoOnlyTexture;

    // 프레임 텍스처 캐시 (프레임이 바뀔 때만 재로드)
    private Texture2D _cachedFrameTexture;
    private string    _cachedFrameKey = "";

    void Start()
    {
        // FrameHolder에서 현재 선택된 프레임 기준으로 합성
        _photoOnlyTexture = MergePhotosOnly(TextureHolder.Instance.GetTextures());
        _baseTexture  = MergeWithFrame(_photoOnlyTexture);
        _finalTexture = _baseTexture;
        compositeImage.texture = _finalTexture;

        // 현재 선택된 프레임 인덱스 동기화
        SyncSelectedFrame();

        ShowTab("frame");
        LoadFrames();
        LoadFilters();
        LoadStickers();
    }

    private void SyncSelectedFrame()
    {
        if (FrameHolder.Instance.IsCustomFrame()) return;

        string currentFrame = FrameHolder.Instance.GetFrame();
        for (int i = 0; i < _frameNames.Length; i++)
        {
            if (_frameNames[i] == currentFrame)
            {
                _selectedFrameIndex = i;
                break;
            }
        }
    }

    private Texture2D MergePhotosOnly(Texture2D[] textures)
    {
        Texture2D result = new Texture2D(FrameW, FrameH, TextureFormat.RGBA32, false);
        Color[] clear = new Color[FrameW * FrameH];
        result.SetPixels(clear); // Color() 기본값이 (0,0,0,0) = clear
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null) continue;
            Texture2D resized = CropAndResize(textures[i], SlotW, SlotH);
            result.SetPixels(SlotX[i], SlotY[i], SlotW, SlotH, resized.GetPixels());
        }
        result.Apply();
        return result;
    }

    // ── 탭 전환 ──────────────────────────────
    public void ShowTab(string tab)
    {
        frameContent.SetActive(tab == "frame");
        filterContent.SetActive(tab == "filter");
        stickerContent.SetActive(tab == "sticker");
    }

    // ── 프레임 로드 ──────────────────────────
    private void LoadFrames()
    {
        foreach (Transform child in frameGrid)
            Destroy(child.gameObject);

        // 기본 프레임
        for (int i = 0; i < _frameNames.Length; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>(
                "Frames/Default/" + _frameNames[i]
            );
            int index = i;
            CreateFrameGridItem(tex, () =>
            {
                _selectedFrameIndex = index;
                FrameHolder.Instance.SetFrame(_frameNames[index]);
                RefreshComposite();
            });
        }

        // 커스텀 프레임
        var customPaths = CustomFrameHolder.Instance.GetFramePaths();
        foreach (var path in customPaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            string captured = path;
            CreateFrameGridItem(tex, () =>
            {
                FrameHolder.Instance.SetCustomFrame(captured);
                RefreshComposite();
            });
        }
    }

    private void CreateFrameGridItem(Texture2D tex, System.Action onClick)
    {
        GameObject item = Instantiate(frameItemPrefab, frameGrid);

        RawImage preview = item.transform.Find("FramePreviewImage")
            .GetComponent<RawImage>();
        preview.texture = tex;

        item.GetComponent<Button>().onClick.AddListener(() => onClick());
    }

    // ── 필터 로드 ──────────────────────────
    private void LoadFilters()
    {
        string[] filterNames = { "원본", "흑백", "세피아", "빈티지", "차갑게", "따뜻하게" };

        foreach (Transform child in filterGrid)
            Destroy(child.gameObject);

        foreach (var name in filterNames)
        {
            GameObject btn = new GameObject(name);
            btn.transform.SetParent(filterGrid);

            RectTransform rt = btn.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;

            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = btn.AddComponent<Button>();

            GameObject label = new GameObject("Label");
            label.transform.SetParent(btn.transform);

            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text      = name;
            tmp.fontSize  = 35;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.black;

            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            string captured = name;
            button.onClick.AddListener(() => ApplyFilter(captured));
        }
    }

    // ── 스티커 로드 ──────────────────────────
    private void LoadStickers()
    {
        foreach (Transform child in stickerGrid)
            Destroy(child.gameObject);

        Texture2D[] stickers = Resources.LoadAll<Texture2D>("DefaultStickers");

        foreach (var sticker in stickers)
        {
            GameObject btn = new GameObject(sticker.name);
            btn.transform.SetParent(stickerGrid);

            RectTransform rt = btn.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;

            RawImage img = btn.AddComponent<RawImage>();
            img.texture = sticker;

            Button button = btn.AddComponent<Button>();
            Texture2D captured = sticker;
            button.onClick.AddListener(() => SpawnSticker(captured));
        }
    }

    // ── 스티커 배치 ──────────────────────────
    private void SpawnSticker(Texture2D tex)
    {
        GameObject element = Instantiate(draggableElementPrefab, decorationLayer);
        RawImage img = element.GetComponent<RawImage>();
        if (img == null) img = element.AddComponent<RawImage>();
        img.texture = tex;
        element.GetComponent<RectTransform>().localPosition = Vector3.zero;
    }

    // ── 필터 적용 ──────────────────────────
    private void ApplyFilter(string filterName)
    {
        Texture2D filtered = new Texture2D(
            _baseTexture.width, _baseTexture.height, TextureFormat.RGBA32, false
        );

        // 수정 ✅ → 사진에만 필터 적용
        Color[] pixels = _photoOnlyTexture.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            switch (filterName)
            {
                case "흑백":
                    float gray = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                    c = new Color(gray, gray, gray, c.a);
                    break;
                case "세피아":
                    float r = c.r * 0.393f + c.g * 0.769f + c.b * 0.189f;
                    float g = c.r * 0.349f + c.g * 0.686f + c.b * 0.168f;
                    float b = c.r * 0.272f + c.g * 0.534f + c.b * 0.131f;
                    c = new Color(r, g, b, c.a);
                    break;
                case "빈티지":
                    c = new Color(
                        Mathf.Clamp01(c.r * 1.1f),
                        Mathf.Clamp01(c.g * 0.9f),
                        Mathf.Clamp01(c.b * 0.7f),
                        c.a
                    );
                    break;
                case "차갑게":
                    c = new Color(
                        Mathf.Clamp01(c.r * 0.9f),
                        Mathf.Clamp01(c.g * 0.95f),
                        Mathf.Clamp01(c.b * 1.2f),
                        c.a
                    );
                    break;
                case "따뜻하게":
                    c = new Color(
                        Mathf.Clamp01(c.r * 1.2f),
                        Mathf.Clamp01(c.g * 1.0f),
                        Mathf.Clamp01(c.b * 0.8f),
                        c.a
                    );
                    break;
                case "원본":
                    break;
            }
            pixels[i] = c;
        }

        filtered.SetPixels(pixels);
        filtered.Apply();
        _finalTexture = MergeWithFrame(filtered);
        compositeImage.texture = _finalTexture;
    }

    private Texture2D MergeWithFrame(Texture2D filteredPhotoTexture)
    {
        Texture2D result = new Texture2D(FrameW, FrameH, TextureFormat.RGBA32, false);
        result.SetPixels(LoadFrameTexture().GetPixels());

        for (int i = 0; i < SlotX.Length; i++)
        {
            Color[] slotPixels = filteredPhotoTexture.GetPixels(
                SlotX[i], SlotY[i], SlotW, SlotH
            );
            result.SetPixels(SlotX[i], SlotY[i], SlotW, SlotH, slotPixels);
        }

        result.Apply();
        return result;
    }

    private Texture2D LoadFrameTexture()
    {
        // 같은 프레임이면 캐시 반환
        string key = FrameHolder.Instance.IsCustomFrame()
            ? FrameHolder.Instance.GetCustomPath()
            : FrameHolder.Instance.GetFrame();

        if (_cachedFrameTexture != null && _cachedFrameKey == key)
            return _cachedFrameTexture;

        Texture2D tex;
        if (FrameHolder.Instance.IsCustomFrame())
        {
            byte[] fileData = File.ReadAllBytes(key);
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(fileData);
        }
        else
        {
            tex = Resources.Load<Texture2D>("Frames/Default/" + key);
        }

        // 읽기 가능한 텍스처로 변환
        RenderTexture rt = RenderTexture.GetTemporary(
            tex.width, tex.height, 0, RenderTextureFormat.ARGB32
        );
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        _cachedFrameKey     = key;
        _cachedFrameTexture = result;
        return result;
    }

    // ── 합성 갱신 ──────────────────────────
    private void RefreshComposite()
    {
        _cachedFrameTexture = null; // 프레임 변경 시 캐시 무효화
        _photoOnlyTexture   = MergePhotosOnly(TextureHolder.Instance.GetTextures());
        _baseTexture        = MergeWithFrame(_photoOnlyTexture);
        _finalTexture       = _baseTexture;
        compositeImage.texture = _finalTexture;
    }

    // ── 저장 ──────────────────────────────
    public void OnSaveButtonPressed()
    {
        byte[] pngData = _finalTexture.EncodeToPNG();
        string path = Path.Combine(
            Application.persistentDataPath, "4cut_result.png"
        );
        File.WriteAllBytes(path, pngData);

        NativeGallery.SaveImageToGallery(
            path,
            "PhotoBooth4Cut",
            "4cut_result.png",
            (success, error) =>
            {
                if (success) Debug.Log("저장 완료");
                else Debug.LogError("저장 실패 : " + error);
            }
        );
    }

    // ── 공유 ──────────────────────────────
    public void OnShareButtonPressed()
    {
        byte[] pngData = _finalTexture.EncodeToPNG();
        string path = Path.Combine(
            Application.persistentDataPath, "4cut_share.png"
        );
        File.WriteAllBytes(path, pngData);

        new NativeShare()
            .AddFile(path)
            .SetSubject("4컷 사진")
            .SetText("PhotoBooth4Cut으로 찍은 4컷 사진!")
            .Share();
    }

    // ── CropAndResize / ResizeTexture ──
    private Texture2D CropAndResize(Texture2D source, int targetWidth, int targetHeight)
    {
        float targetRatio = (float)targetWidth / targetHeight;
        float sourceRatio = (float)source.width / source.height;

        int cropX, cropY, cropW, cropH;

        if (sourceRatio > targetRatio)
        {
            cropH = source.height;
            cropW = Mathf.RoundToInt(cropH * targetRatio);
            cropX = (source.width - cropW) / 2;
            cropY = 0;
        }
        else
        {
            cropW = source.width;
            cropH = Mathf.RoundToInt(cropW / targetRatio);
            cropX = 0;
            cropY = (source.height - cropH) / 2;
        }

        Texture2D cropped = new Texture2D(cropW, cropH);
        cropped.SetPixels(source.GetPixels(cropX, cropY, cropW, cropH));
        cropped.Apply();

        return ResizeTexture(cropped, targetWidth, targetHeight);
    }

    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height);
        result.filterMode = FilterMode.Bilinear;
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}