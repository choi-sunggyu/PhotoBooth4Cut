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
        // TextureHolder에서 촬영된 사진 가져오기
        _capturedTextures = TextureHolder.Instance.GetTextures();
        _finalTexture = MergeTextures(_capturedTextures);
        resultImage.texture = _finalTexture;
    }

    private Texture2D MergeTextures(Texture2D[] textures)
    {
        int slotWidth  = 400;
        int slotHeight = 600;
        int padding    = 10;

        int totalWidth  = slotWidth + padding * 2;
        int totalHeight = slotHeight * 4 + padding * 5;

        Texture2D result = new Texture2D(totalWidth, totalHeight);

        // 배경 흰색으로 초기화
        Color[] bg = new Color[totalWidth * totalHeight];
        for (int i = 0; i < bg.Length; i++) bg[i] = Color.white;
        result.SetPixels(bg);

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null) continue;

            // 각 슬롯 크기로 리사이즈
            Texture2D resized = ResizeTexture(textures[i], slotWidth, slotHeight);

            int x = padding;
            // 위에서부터 채워야 하므로 y 계산 반전
            int y = totalHeight - (slotHeight + padding) * (i + 1);

            result.SetPixels(x, y, slotWidth, slotHeight, resized.GetPixels());
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