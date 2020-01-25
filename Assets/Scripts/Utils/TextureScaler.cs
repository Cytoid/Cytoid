using UnityEngine;

/// A unility class with functions to scale Texture2D Data.
///
/// Scale is performed on the GPU using RTT, so it's blazing fast.
/// Setting up and Getting back the texture data is the bottleneck.
/// But Scaling itself costs only 1 draw call and 1 RTT State setup!
/// WARNING: This script override the RTT Setup! (It sets a RTT!)        
///
/// Note: This scaler does NOT support aspect ratio based scaling. You will have to do it yourself!
/// It supports Alpha, but you will have to divide by alpha in your shaders,
/// because of premultiplied alpha effect. Or you should use blend modes.
public class TextureScaler
{
    /// <summary>
    ///     Returns a scaled copy of given texture.
    /// </summary>
    /// <param name="tex">Source texure to scale</param>
    /// <param name="width">Destination texture width</param>
    /// <param name="height">Destination texture height</param>
    /// <param name="mode">Filtering mode</param>
    public static Texture2D Scaled(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(src, width, height, mode);

        //Get rendered data back to a new texture
        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
        result.Resize(width, height);
        result.ReadPixels(texR, 0, 0, true);
        return result;
    }

    /// <summary>
    /// Scales the texture data of the given texture.
    /// </summary>
    /// <param name="tex">Texure to scale</param>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    /// <param name="mode">Filtering mode</param>
    public static void Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(tex, width, height, mode);

        // Update new texture
        tex.Resize(width, height);
        tex.ReadPixels(texR, 0, 0, true);
        tex.Apply(true); //Remove this if you hate us applying textures for you :)
    }
    
    public static Texture2D FitCrop(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        var result = new Texture2D(width, height, TextureFormat.ARGB32, true);
        result.Resize(width, height);
        var targetRatio = width * 1.0f / height;
        var ratio = tex.width * 1.0f / tex.height;
        Debug.Log($"Width: {width}, Height: {height}, Texture width: {tex.width}, Texture height: {tex.height}, Target ratio: {targetRatio}, Texture ratio: {ratio}");
        Rect texR;
        if (targetRatio < ratio)
        {
            var toWidth = tex.width * height * 1.0f / tex.height;
            texR = new Rect((int) ((toWidth - width) / 2), 0, width, height);
            _gpu_scale(tex, (int) toWidth, height, mode);
            Debug.Log($"To width: {toWidth}, Rect: {texR}");
            result.ReadPixels(texR, 0, 0, true);
        }
        else
        {
            var toHeight = tex.height * width * 1.0f / tex.width;
            texR = new Rect(0, (int) ((toHeight - height) / 2), width, height);
            _gpu_scale(tex, width, (int) toHeight, mode);
            Debug.Log($"To height: {toHeight}, Rect: {texR}");
            result.ReadPixels(texR, 0, 0, true);
        }

        return result;
    }

    // Internal unility that renders the source texture into the RTT - the scaling method itself.
    static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
    {
        //We need the source texture in VRAM because we render with it
        src.filterMode = fmode;
        src.Apply(true);

        //Using RTT for best quality and performance. Thanks, Unity 5
        RenderTexture rtt = new RenderTexture(width, height, 32);

        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }
}