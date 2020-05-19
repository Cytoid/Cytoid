using UnityEngine.UI;

public class ExtendedSlider : Slider
{
    public void SetWithoutCallback(float sliderValue)
    {
        Set(sliderValue, false);
    }
}