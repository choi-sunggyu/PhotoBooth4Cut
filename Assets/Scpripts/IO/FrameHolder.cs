using UnityEngine;

public class FrameHolder : MonoBehaviour
{
    public static FrameHolder Instance;

    private string _selectedFrameName = "frame_01";

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
    }

    public string GetFrame()
    {
        return _selectedFrameName;
    }
}