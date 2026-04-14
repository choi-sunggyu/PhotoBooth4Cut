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
        int frameWidth  = 1060;
        int frameHeight = 3187;
        int slotWidth   = 940;
        int slotHeight  = 629;
        int slotX       = 60;

        // 슬롯 Unity Y 좌표 (아래→위)
        int[] slotYPositions = { 2478, 1819, 1160, 501 };

        // 프레임 PNG 로드
        Texture2D frameTexture = Resources.Load<Texture2D>("Frames/Default/frame_01");

        // 결과 텍스처 생성
        Texture2D result = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);

        // 프레임을 베이스로 복사
        result.SetPixels(frameTexture.GetPixels());

        // 각 슬롯에 사진 합성
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null) continue;

            Texture2D resized = ResizeTexture(textures[i], slotWidth, slotHeight);
            result.SetPixels(slotX, slotYPositions[i], slotWidth, slotHeight, resized.GetPixels());
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