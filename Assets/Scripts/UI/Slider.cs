namespace Cytoid.UI
{
    public class Slider : UnityEngine.UI.Slider
    {
        public void SetDirectly(float sliderValue)
        {
            Set(sliderValue, false);
        }
    }
}