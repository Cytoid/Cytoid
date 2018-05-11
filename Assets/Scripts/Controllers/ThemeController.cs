using System;
using UnityEngine;

public class ThemeController : SingletonMonoBehavior<ThemeController>
{
    public Color perfectColor;
    public Color greatColor;
    public Color goodColor;
    public Color badColor;
    public Color missColor;

    public Color ringColor1;
    public Color fillColor1;

    public Color ringColor2;
    public Color fillColor2;

    public Color ringColor3;
    public Color fillColor3;
    
    public Color ringColor4;
    public Color fillColor4;

    public void Init(Level level)
    {
        try
        {
            ringColor1 = Convert.HexToColor(PlayerPrefs.GetString("ring_color"));
            ringColor2 = Convert.HexToColor(PlayerPrefs.GetString("ring_color_alt"));
            ringColor3 = Convert.HexToColor("#FFFFFF");
            ringColor4 = Convert.HexToColor("#FFFFFF");
            fillColor1 = Convert.HexToColor(PlayerPrefs.GetString("fill_color"));
            fillColor2 = Convert.HexToColor(PlayerPrefs.GetString("fill_color_alt"));
            fillColor3 = Convert.HexToColor("#FFFFFF");
            fillColor4 = Convert.HexToColor("#FFFFFF");
            if (level.theme == null) return;
            if (level.theme.ring_color_1 != null)
            {
                ringColor1 = Convert.HexToColor(level.theme.ring_color_1);
            }
            if (level.theme.fill_color_1 != null)
            {
                fillColor1 = Convert.HexToColor(level.theme.fill_color_1);
            }
            if (level.theme.ring_color_2 != null)
            {
                ringColor2 = Convert.HexToColor(level.theme.ring_color_2);
            }
            if (level.theme.fill_color_2 != null)
            {
                fillColor2 = Convert.HexToColor(level.theme.fill_color_2);
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

}