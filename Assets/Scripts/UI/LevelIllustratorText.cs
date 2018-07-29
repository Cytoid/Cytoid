namespace Cytoid.UI
{
    public class LevelIllustratorText : TextBehavior
    {
        
        private void Update()
        {
            var level = LevelSelectionController.Instance.LoadedLevel;
            if (level == null) return;
            
            Text.text = level.illustrator ?? "Unknown";
        }
    }
}