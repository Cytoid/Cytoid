namespace Cytoid.UI
{
    public class LevelIdText : TextBehavior
    {
        
        private void Update()
        {
            var level = LevelSelectionController.Instance.LoadedLevel;
            if (level == null) return;
            
            Text.text = (level.id ?? "Unknown") + " (v" + level.version + ")";
        }
        
    }
}