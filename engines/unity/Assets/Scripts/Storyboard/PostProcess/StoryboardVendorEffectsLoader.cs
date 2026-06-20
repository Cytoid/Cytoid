using System;
using System.Reflection;
using Cytoid.Storyboard;

namespace Cytoid.Storyboard.PostProcess
{
    internal static class StoryboardVendorEffectsLoader
    {
        public static bool TryRegister(StoryboardRendererProvider provider)
        {
            var type = VendorStoryboardInstall.ResolveBootstrapType();
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
