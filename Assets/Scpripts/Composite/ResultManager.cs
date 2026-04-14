using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ResultManager : MonoBehaviour
{
    [Header("UI 연결")]
    public RawImage resultImage;

    private Texture2D _finalTexture;
    private Texture2D[] _capturedTextures;

    void Start()
    {
        Debug.Log("ResultManager Start 실행됨"); // 추가
        // TextureHolder에서 촬영된 사진 가져오기
        _capturedTextures = TextureHolder.Instance.GetTextures();
        _finalTexture = MergeTextures(_capturedTextures);
        resultImage.texture = _finalTexture;
    }

    private Texture2D MergeTextures(Texture2D[] textures)
    {
        int frameWidth  = 1200;
        int frameHeight = 1800;
        int slotWidth   = 550;
        int slotHeight  = 715;

        int[] slotX = { 40,  610, 40,  610  };
        int[] slotY = { 1035, 1035, 300, 300 };

        Texture2D frameSource = Resources.Load<Texture2D>(
            "Frames/Default/" + FrameHolder.Instance.GetFrame()
        );
        RenderTexture rt = RenderTexture.GetTemporary(frameWidth, frameHeight);
        Graphics.Blit(frameSource, rt);
        RenderTexture.active = rt;

        Texture2D frameTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
        frameTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0);
        frameTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Texture2D result = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
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

    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private Texture2D CropAndResize(Texture2D source, int targetWidth, int targetHeight)
    {
        // 목표 비율
        float targetRatio = (float)targetWidth / targetHeight;
        float sourceRatio = (float)source.width / source.height;

        int cropX, cropY, cropW, cropH;

        if (sourceRatio > targetRatio)
        {
            // 원본이 더 넓음 → 좌우 크롭
            cropH = source.height;
            cropW = Mathf.RoundToInt(cropH * targetRatio);
            cropX = (source.width - cropW) / 2;
            cropY = 0;
        }
        else
        {
            // 원본이 더 높음 → 상하 크롭
            cropW = source.width;
            cropH = Mathf.RoundToInt(cropW / targetRatio);
            cropX = 0;
            cropY = (source.height - cropH) / 2;
        }

        // 크롭 후 리사이즈
        Texture2D cropped = new Texture2D(cropW, cropH);
        cropped.SetPixels(source.GetPixels(cropX, cropY, cropW, cropH));
        cropped.Apply();

        return ResizeTexture(cropped, targetWidth, targetHeight);
    }

    public void OnSaveButtonPressed()
    {
        byte[] pngData = _finalTexture.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, "4cut_result.png");
        File.WriteAllBytes(path, pngData);

        NativeGallery.SaveImageToGallery(
            path,
            "PhotoBooth4Cut",
            "4cut_result.png",
            (success, error) =>
            {
                if (success)
                    Debug.Log("저장 완료");
                else
                    Debug.LogError("저장 실패 : " + error);
            }
        );
    }

    public void OnShareButtonPressed()
    {
        byte[] pngData = _finalTexture.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, "4cut_share.png");
        File.WriteAllBytes(path, pngData);

        new NativeShare()
            .AddFile(path)
            .SetSubject("4컷 사진")
            .SetText("PhotoBooth4Cut으로 찍은 4컷 사진이야!")
            .Share();
    }
}