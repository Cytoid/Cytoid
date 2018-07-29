namespace Cytoid.UI
{
    public class LevelTitleText : TextBehavior
    {
        
        private void Update()
        {
            var level = LevelSelectionController.Instance.LoadedLevel;
            if (level == null) return;
            
            Text.text = level.title ?? "Unknown";
        }
    }
}