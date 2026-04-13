using UnityEngine;

public class TextureHolder : MonoBehaviour
{
    public static TextureHolder Instance;

    private Texture2D[] _textures;

    void Awake()
    {
        // 씬이 바뀌어도 파괴되지 않음
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetTextures(Texture2D[] textures)
    {
        _textures = textures;
    }

    public Texture2D[] GetTextures()
    {
        return _textures;
    }
}