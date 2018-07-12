namespace Cytoid.UI
{
    public class DirectSlider : UnityEngine.UI.Slider
    {
        public void SetDirectly(float sliderValue)
        {
            Set(sliderValue, false);
        }
    }
}