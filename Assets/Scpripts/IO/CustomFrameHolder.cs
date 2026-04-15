using UnityEngine;
using System.Collections.Generic;

public class CustomFrameHolder : MonoBehaviour
{
    public static CustomFrameHolder Instance;

    private List<string> _customFramePaths = new List<string>();
    private const string PREFS_KEY = "CustomFramePaths";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromPrefs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFrame(string path)
    {
        string norm = Normalize(path);
        if (!_customFramePaths.Exists(p => Normalize(p) == norm))
        {
            _customFramePaths.Add(path);
            SaveToPrefs();
        }
    }

    public void RemoveFramePath(string path)
    {
        string norm = Normalize(path);
        int idx = _customFramePaths.FindIndex(p => Normalize(p) == norm);
        if (idx < 0)
        {
            Debug.LogWarning($"[CustomFrameHolder] 삭제 실패 - 경로 없음: {path}");
            return;
        }
        _customFramePaths.RemoveAt(idx);
        SaveToPrefs();
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').ToLowerInvariant();

    public List<string> GetFramePaths()
    {
        return _customFramePaths;
    }

    private void SaveToPrefs()
    {
        string joined = string.Join("|", _customFramePaths);
        PlayerPrefs.SetString(PREFS_KEY, joined);
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        string saved = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(saved)) return;

        string[] paths = saved.Split('|');
        foreach (var path in paths)
        {
            if (!string.IsNullOrEmpty(path) &&
                System.IO.File.Exists(path))
            {
                _customFramePaths.Add(path);
            }
        }
    }
}