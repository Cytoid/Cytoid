namespace Cytoid.Storyboard.PostProcess
{
    public static class StoryboardEffects
    {
        public static IStoryboardEffects Current { get; internal set; }

        public static bool HasVendorBackend =>
            Current != null && Current.GetType().FullName == "Cytoid.Storyboard.Vendor.VendorStoryboardEffects";
    }
}
