using System;
using System.Reflection;
using Cytoid.Storyboard;

namespace Cytoid.Storyboard.PostProcess
{
    internal static class StoryboardVendorEffectsLoader
    {
        const string BootstrapTypeName = "Cytoid.Storyboard.Vendor.VendorStoryboardEffectsBootstrap";

        public static bool TryRegister(StoryboardRendererProvider provider)
        {
            var type = Type.GetType(BootstrapTypeName);
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
