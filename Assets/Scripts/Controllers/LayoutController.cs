using UnityEngine;

public class LayoutController : SingletonMonoBehavior<LayoutController>
{

    public float PlayAreaHeight
    {
        get { return Camera.main.orthographicSize * 2.0f * 0.67f; }
    }

    public float PlayAreaWidth
    {
        get { return Camera.main.orthographicSize * 2.0f * 0.8f / Screen.height * Screen.width; }
    }

    public float PlayAreaVerticalMargin
    {
        get { return (Camera.main.orthographicSize * 2.0f * 0.15f) - Camera.main.orthographicSize - Camera.main.orthographicSize * 2.0f * 0.02f; }
    }

    public float PlayAreaHorizontalMargin
    {
        get
        {
            return (Camera.main.orthographicSize * 2.0f * 0.1f / Screen.height * Screen.width) -
                   Camera.main.orthographicSize / Screen.height * Screen.width;
        }
    }

    public float NoteSize
    {
        get { return Camera.main.orthographicSize * 2.0f * 0.22f * 0.9f; }
    }

    public float NoteChainSize
    {
        get { return Camera.main.orthographicSize * 2.0f * 0.08f * 1.25f; }
    }

    public float NoteChainHeadSize
    {
        get { return NoteChainSize * 2f; }
    }

    public float ScannerHeight
    {
        get { return Camera.main.orthographicSize * 2.0f * 0.012f; }
    }

}