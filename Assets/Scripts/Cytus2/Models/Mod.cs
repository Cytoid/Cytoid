using Cytus2.Controllers;

namespace Cytus2.Models
{
    public enum Mod
    {
        FlipX,
        FlipY,
        FlipAll,
        
        Slow,
        Fast,
        
        FC,
        AP,
        Hard,
        ExHard,
        
        HideScanline,
        HideNotes,
        
        AutoDrag,
        AutoHold,
        AutoFlick,
        Auto
    }

    public static class ModExtensions
    {

        public static bool IsEnabled(this Mod mod)
        {
            return Game.Instance.Play.Mods.Contains(mod);
        }
        
    }
    
}