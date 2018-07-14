using UnityEngine;
using UnityEngine.UI;

public class AlphaMask : MonoBehaviour
{
    public bool willFadeIn;
    public bool willFadeOut;
    public float max = 1f;
    public float min = 0f;
    public float speed = 0.05f;

    private Image image;

    public bool IsFading
    {
        get { return willFadeIn || willFadeOut; }
    }

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void FixedUpdate()
    {
        if (willFadeIn)
        {
            var newColor = image.color;
            newColor.a += speed;
            image.color = newColor;
            if (newColor.a >= max)
            {
                newColor.a = max;
                image.color = newColor;
                willFadeIn = false;
            }
        }
        else if (willFadeOut)
        {
            var newColor = image.color;
            newColor.a -= speed;
            image.color = newColor;
            if (newColor.a <= min)
            {
                newColor.a = min;
                image.color = newColor;
                willFadeOut = false;
            }
        }
    }
}