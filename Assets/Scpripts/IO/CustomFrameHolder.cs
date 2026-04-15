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
            LoadFromPrefs(); // 저장된 경로 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFrame(string path)
    {
        if (!_customFramePaths.Contains(path))
        {
            _customFramePaths.Add(path);
            SaveToPrefs();
        }
    }

    public List<string> GetFramePaths()
    {
        return _customFramePaths;
    }

    private void SaveToPrefs()
    {
        // 경로들을 | 로 구분해서 저장
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
            // 실제 파일 존재 여부 확인
            if (!string.IsNullOrEmpty(path) &&
                System.IO.File.Exists(path))
            {
                _customFramePaths.Add(path);
            }
        }
    }
}