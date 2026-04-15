using UnityEngine;

public class FrameHolder : MonoBehaviour
{
    public static FrameHolder Instance;

    private string _selectedFrameName = "frame_01";
    private string _customFramePath   = "";
    private bool   _isCustomFrame     = false;

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

    public void SetFrame(string frameName)
    {
        _selectedFrameName = frameName;
        _isCustomFrame     = false;
        _customFramePath   = "";
    }

    public void SetCustomFrame(string path)
    {
        _customFramePath = path;
        _isCustomFrame   = true;
    }

    public string GetFrame()       { return _selectedFrameName; }
    public string GetCustomPath()  { return _customFramePath; }
    public bool IsCustomFrame()    { return _isCustomFrame; }
}