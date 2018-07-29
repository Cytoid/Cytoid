namespace Cytoid.UI
{
    public class LevelCharterText : TextBehavior
    {
        
        private void Update()
        {
            var level = LevelSelectionController.Instance.LoadedLevel;
            if (level == null) return;
            
            Text.text = level.charter ?? "Unknown";
        }
    }
}