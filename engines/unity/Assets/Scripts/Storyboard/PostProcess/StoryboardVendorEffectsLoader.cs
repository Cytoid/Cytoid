using System;
using System.Linq;
using System.Reflection;
using Cytoid.Storyboard;

namespace Cytoid.Storyboard.PostProcess
{
    internal static class StoryboardVendorEffectsLoader
    {
        const string BootstrapTypeName = "Cytoid.Storyboard.Vendor.VendorStoryboardEffectsBootstrap";

        public static bool TryRegister(StoryboardRendererProvider provider)
        {
            // Type.GetType with a bare name searches only the calling assembly and
            // mscorlib, which fails for cross-assembly lookups in built players
            // (IL2CPP/AOT). Fall back to an AppDomain scan so the vendor backend is
            // resolved consistently in Editor Play Mode and in exported plugins.
            var type = Type.GetType(BootstrapTypeName)
                       ?? AppDomain.CurrentDomain.GetAssemblies()
                           .Select(a => a.GetType(BootstrapTypeName))
                           .FirstOrDefault(t => t != null);
            if (type == null)
                return false;

            var register = type.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
            if (register == null)
                return false;

            register.Invoke(null, new object[] { provider });
            return StoryboardEffects.Current != null;
        }
    }
}
