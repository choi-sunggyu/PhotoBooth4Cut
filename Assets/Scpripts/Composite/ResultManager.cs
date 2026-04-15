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
    public Slider contrastSlider;

    [Header("스티커")]
    public Transform stickerGrid;    // StickerGrid 연결
    public GameObject draggableElementPrefab;

    [Header("버튼")]
    public Button saveButton;
    public Button shareButton;

    private Texture2D _finalTexture;
    private Texture2D _baseTexture;
    private string[] _frameNames = { "frame_01", "frame_02" };
    private int _selectedFrameIndex = 0;

    private Texture2D _photoOnlyTexture;

    void Start()
    {
        // 사진만 따로 합성 보관
        _photoOnlyTexture = MergePhotosOnly(
            TextureHolder.Instance.GetTextures()
        );
        _baseTexture  = MergeTextures(TextureHolder.Instance.GetTextures());
        _finalTexture = _baseTexture;
        compositeImage.texture = _finalTexture;

        // 기본 탭 → 프레임
        ShowTab("frame");

        LoadFrames();
        LoadFilters();
        LoadStickers();

        // brightnessSlider.minValue = -1f;
        // brightnessSlider.maxValue = 1f;
        // brightnessSlider.value    = 0f;

        contrastSlider.minValue = 0.5f;
        contrastSlider.maxValue = 2f;
        contrastSlider.value    = 1f;

        //brightnessSlider.onValueChanged.AddListener(OnFilterChanged);
        contrastSlider.onValueChanged.AddListener(OnFilterChanged);
    }

    private Texture2D MergePhotosOnly(Texture2D[] textures)
    {
        int frameWidth  = 1200;
        int frameHeight = 1800;
        int slotWidth   = 550;
        int slotHeight  = 715;

        int[] slotX = { 40,  610, 40,  610  };
        int[] slotY = { 1035, 1035, 300, 300 };

        // 투명 배경으로 시작 (프레임 없음)
        Texture2D result = new Texture2D(
            frameWidth, frameHeight, TextureFormat.RGBA32, false
        );
        Color[] clear = new Color[frameWidth * frameHeight];
        for (int i = 0; i < clear.Length; i++)
            clear[i] = Color.clear;
        result.SetPixels(clear);

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null) continue;
            Texture2D resized = CropAndResize(textures[i], slotWidth, slotHeight);
            result.SetPixels(slotX[i], slotY[i], slotWidth, slotHeight, resized.GetPixels());
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

        for (int i = 0; i < _frameNames.Length; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>(
                "Frames/Default/" + _frameNames[i]
            );

            GameObject item = Instantiate(frameItemPrefab, frameGrid);
            RawImage preview = item.transform.Find("FramePreviewImage")
                .GetComponent<RawImage>();
            preview.texture = tex;

            int index = i;
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                _selectedFrameIndex = index;
                FrameHolder.Instance.SetFrame(_frameNames[index]);
                RefreshComposite();
            });
        }
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
            _baseTexture.width,
            _baseTexture.height
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

    // ── 밝기/대비 조절 ──────────────────────
    private void OnFilterChanged(float value)
    {
        float contrast = contrastSlider.value;

        // 사진에만 대비 적용
        Color[] pixels = _photoOnlyTexture.GetPixels();
        Color[] result = new Color[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            if (c.a > 0) // 투명 영역 제외
            {
                c = new Color(
                    Mathf.Clamp01((c.r - 0.5f) * contrast + 0.5f),
                    Mathf.Clamp01((c.g - 0.5f) * contrast + 0.5f),
                    Mathf.Clamp01((c.b - 0.5f) * contrast + 0.5f),
                    c.a
                );
            }
            result[i] = c;
        }

        Texture2D filteredPhoto = new Texture2D(
            _photoOnlyTexture.width,
            _photoOnlyTexture.height,
            TextureFormat.RGBA32, false
        );
        filteredPhoto.SetPixels(result);
        filteredPhoto.Apply();

        // 프레임과 다시 합성
        _finalTexture = MergeWithFrame(filteredPhoto);
        compositeImage.texture = _finalTexture;
    }

    private Texture2D MergeWithFrame(Texture2D photoTexture)
    {
        int frameWidth  = 1200;
        int frameHeight = 1800;

        Texture2D frameSource;

        if (FrameHolder.Instance.IsCustomFrame())
        {
            byte[] fileData = System.IO.File.ReadAllBytes(
                FrameHolder.Instance.GetCustomPath()
            );
            frameSource = new Texture2D(2, 2);
            frameSource.LoadImage(fileData);
        }
        else
        {
            frameSource = Resources.Load<Texture2D>(
                "Frames/Default/" + FrameHolder.Instance.GetFrame()
            );
        }

        RenderTexture rt = RenderTexture.GetTemporary(frameWidth, frameHeight);
        Graphics.Blit(frameSource, rt);
        RenderTexture.active = rt;

        Texture2D frameTexture = new Texture2D(
            frameWidth, frameHeight, TextureFormat.RGBA32, false
        );
        frameTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0);
        frameTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // 사진을 먼저 깔고 프레임을 위에 올리기
        Color[] photoPixels = photoTexture.GetPixels();
        Color[] framePixels = frameTexture.GetPixels();
        Color[] final       = new Color[photoPixels.Length];

        for (int i = 0; i < final.Length; i++)
        {
            // 프레임 픽셀이 투명하면 사진 픽셀 사용
            if (framePixels[i].a < 0.1f)
                final[i] = photoPixels[i];
            else
                final[i] = framePixels[i];
        }

        Texture2D result = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
        result.SetPixels(final);
        result.Apply();
        return result;
    }

    // ── 합성 갱신 ──────────────────────────
    private void RefreshComposite()
    {
        _photoOnlyTexture = MergePhotosOnly(TextureHolder.Instance.GetTextures());
        _baseTexture      = MergeTextures(TextureHolder.Instance.GetTextures());
        _finalTexture     = _baseTexture;
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

    // ── MergeTextures / CropAndResize / ResizeTexture ──
    private Texture2D MergeTextures(Texture2D[] textures)
    {
        int frameWidth  = 1200;
        int frameHeight = 1800;
        int slotWidth   = 550;
        int slotHeight  = 715;

        int[] slotX = { 40,  610, 40,  610  };
        int[] slotY = { 1035, 1035, 300, 300 };

        Texture2D frameSource;

        if (FrameHolder.Instance.IsCustomFrame())
        {
            byte[] fileData = System.IO.File.ReadAllBytes(
                FrameHolder.Instance.GetCustomPath()
            );
            frameSource = new Texture2D(2, 2);
            frameSource.LoadImage(fileData);
        }
        else
        {
            frameSource = Resources.Load<Texture2D>(
                "Frames/Default/" + FrameHolder.Instance.GetFrame()
            );
        }

        RenderTexture rt = RenderTexture.GetTemporary(frameWidth, frameHeight);
        Graphics.Blit(frameSource, rt);
        RenderTexture.active = rt;

        Texture2D frameTexture = new Texture2D(
            frameWidth, frameHeight, TextureFormat.RGBA32, false
        );
        frameTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0);
        frameTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Texture2D result = new Texture2D(
            frameWidth, frameHeight, TextureFormat.RGBA32, false
        );
        result.SetPixels(frameTexture.GetPixels());

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null) continue;
            Texture2D resized = CropAndResize(textures[i], slotWidth, slotHeight);
            result.SetPixels(slotX[i], slotY[i], slotWidth, slotHeight, resized.GetPixels());
        }

        result.Apply();
        return result;
    }

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