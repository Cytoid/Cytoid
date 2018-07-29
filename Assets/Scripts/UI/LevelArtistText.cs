namespace Cytoid.UI
{
    public class LevelArtistText : TextBehavior
    {
        
        private void Update()
        {
            var level = LevelSelectionController.Instance.LoadedLevel;
            if (level == null) return;
            
            Text.text = level.artist ?? "Unknown";
        }
    }
}