// UnityEngine half of the compiled skin image record (the pure half — src,
// bytes, size and the packed UV floats — lives in SkinIr.cs so the pure-C#
// compile, Tools/SkinProbe, stays engine-free). The atlas page is assigned on
// the main thread by FlatSkinRuntime.BuildAtlas; UvRect surfaces the packed
// floats as a Rect in Unity texture space (y-up, v0 = image bottom).

using UnityEngine;

public sealed partial class SkinImage
{
    public Texture2D Texture; // atlas page assigned on the main thread

    /// <summary>Atlas rect once packed — empty until FlatSkinRuntime.BuildAtlas ran.</summary>
    public Rect UvRect
    {
        get => new Rect(U0, V0, U1 - U0, V1 - V0);
        set
        {
            U0 = value.x;
            V0 = value.y;
            U1 = value.x + value.width;
            V1 = value.y + value.height;
        }
    }
}
