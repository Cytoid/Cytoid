namespace Cytus2.Models
{

    public class GameOptions : SingletonMonoBehavior<GameOptions>
    {

        public bool IsRanked = true;
        public bool WillAutoPlay = false;
        public bool ShowEarlyLateIndicator = true;
        public bool UseAndroidNativeAudio = false;
        public float HitboxMultiplier = 1.3333f;
        public float StartAt;
        public float ChartOffset;

    }

}