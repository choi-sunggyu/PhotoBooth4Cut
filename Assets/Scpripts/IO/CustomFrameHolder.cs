using UnityEngine;
using System.Collections.Generic;

public class CustomFrameHolder : MonoBehaviour
{
    public static CustomFrameHolder Instance;

    private List<string> _customFramePaths = new List<string>();

    void Awake()
    {
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

    public void AddFrame(string path)
    {
        if (!_customFramePaths.Contains(path))
            _customFramePaths.Add(path);
    }

    public List<string> GetFramePaths()
    {
        return _customFramePaths;
    }
}