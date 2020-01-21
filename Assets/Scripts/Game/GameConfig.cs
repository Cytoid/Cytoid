using System;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig
{
    public bool IsCalibration;
    
    public float ChartOffset;
    public float NoteHitboxMultiplier;
    public bool UseScannerSmoothing;

    public float GlobalNoteOpacityMultiplier = 1f;
    public Color GlobalRingColorOverride;
    public readonly Dictionary<NoteType, Color[]> GlobalFillColorsOverride = new Dictionary<NoteType, Color[]>();

    public float NoteSizeMultiplier;
    public readonly Dictionary<NoteType, float> NoteSizes = new Dictionary<NoteType, float>();
    public readonly Dictionary<NoteType, Color[]> NoteRingColors = new Dictionary<NoteType, Color[]>();
    public readonly Dictionary<NoteType, Color[]> NoteFillColors = new Dictionary<NoteType, Color[]>();
    public Dictionary<NoteGrade, Color> NoteGradeEffectColors = new Dictionary<NoteGrade, Color>();

    public GameConfig(Game game)
    {
        game.onGameLoaded.AddListener(OnGameLoaded);
    }

    public static readonly Dictionary<NoteType, int[]> NoteColorChartOverrideMapping = new Dictionary<NoteType, int[]>
    {
        {NoteType.Click, new[] {0, 1}},
        {NoteType.DragHead, new[] {2, 3}},
        {NoteType.DragChild, new[] {2, 3}},
        {NoteType.Hold, new[] {4, 5}},
        {NoteType.LongHold, new[] {6, 7}},
        {NoteType.Flick, new[] {8, 9}}
    };

    public void OnGameLoaded(Game game)
    {
        var chart = game.Chart;

        ChartOffset = Context.LocalPlayer.BaseNoteOffset + Context.LocalPlayer.GetLevelNoteOffset(game.Level.Id);
        if (DetectHeadset.Detect()) ChartOffset += Context.LocalPlayer.HeadsetNoteOffset;

        NoteHitboxMultiplier = Context.LocalPlayer.UseLargerHitboxes ? 1.5555f : 1.3333f;
        UseScannerSmoothing = true;

        GlobalRingColorOverride = chart.Model.ring_color?.ToColor() ?? Color.clear;
        foreach (NoteType type in Enum.GetValues(typeof(NoteType)))
        {
            GlobalFillColorsOverride[type] = new[]
            {
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][0]]?.ToColor() ?? Color.clear,
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][1]]?.ToColor() ?? Color.clear
            };
        }

        var playerNoteSizeOffset = Context.LocalPlayer.NoteSize;
        NoteSizeMultiplier = (float) chart.Model.size * (1 + playerNoteSizeOffset);

        NoteSizes[NoteType.Click] = (Camera.main.orthographicSize * 2.0f) * (7.0f / 9.0f) / 5.0f * 1.2675f;
        NoteSizes[NoteType.DragHead] = NoteSizes[NoteType.Click] * 0.8f;
        NoteSizes[NoteType.DragChild] = NoteSizes[NoteType.Click] * 0.65f;
        NoteSizes[NoteType.Hold] = NoteSizes[NoteType.Click];
        NoteSizes[NoteType.LongHold] = NoteSizes[NoteType.Click];
        NoteSizes[NoteType.Flick] = NoteSizes[NoteType.Click] * 1.125f;

        foreach (NoteType type in Enum.GetValues(typeof(NoteType)))
        {
            NoteRingColors[type] = new[]
            {
                chart.Model.ring_color?.ToColor() ?? Context.LocalPlayer.GetRingColor(type, false),
                chart.Model.ring_color?.ToColor() ?? Context.LocalPlayer.GetRingColor(type, true)
            };
            NoteFillColors[type] = new[]
            {
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][0]]?.ToColor() ??
                Context.LocalPlayer.GetFillColor(type, false),
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][1]]?.ToColor() ??
                Context.LocalPlayer.GetFillColor(type, true)
            };
        }

        NoteGradeEffectColors = new Dictionary<NoteGrade, Color>
        {
            {NoteGrade.Perfect, "#5BC0EB".ToColor()},
            {NoteGrade.Great, "#FDE74C".ToColor()},
            {NoteGrade.Good, "#9BC53D".ToColor()},
            {NoteGrade.Bad, "#E55934".ToColor()},
            {NoteGrade.Miss, "#333333".ToColor()},
        };

        if (game.Storyboard != null)
        {
            if (game.Storyboard.Controllers.Count > 0)
            {
                game.Storyboard.Config.UseEffects = true;
                switch (Context.LocalPlayer.GraphicsLevel)
                {
                    case "high":
                        UnityEngine.Screen.SetResolution(Context.InitialWidth, Context.InitialHeight, true);
                        break;
                    case "medium":
                        UnityEngine.Screen.SetResolution((int) (Context.InitialWidth * 0.7f),
                            (int) (Context.InitialHeight * 0.7f), true);
                        break;
                    case "low":
                        UnityEngine.Screen.SetResolution((int) (Context.InitialWidth * 0.5f),
                            (int) (Context.InitialHeight * 0.5f), true);
                        break;
                    case "none":
                        game.Storyboard.Config.UseEffects = false;
                        break;
                }
            }
        }

        if (Context.LocalPlayer.LowerResolution)
        {
            UnityEngine.Screen.SetResolution((int) (Context.InitialWidth * 0.5f),
                (int) (Context.InitialHeight * 0.5f), true);
        }
    }

    public Color GetRingColor(ChartModel.Note note)
    {
        return note.direction > 0 ? NoteRingColors[(NoteType) note.type][0] : NoteRingColors[(NoteType) note.type][1];
    }

    public Color GetFillColor(ChartModel.Note note)
    {
        if ((NoteType) note.type == NoteType.DragChild) return GetRingColor(note); // Special case: drag child
        return note.direction > 0 ? NoteFillColors[(NoteType) note.type][0] : NoteFillColors[(NoteType) note.type][1];
    }

    public Color GetRingColorOverride(ChartModel.Note note)
    {
        return GlobalRingColorOverride;
    }

    public Color GetFillColorOverride(ChartModel.Note note)
    {
        var alt = note.direction > 0;
        if (note.is_forward) alt = !alt;
        return alt
            ? GlobalFillColorsOverride[(NoteType) note.type][0]
            : GlobalFillColorsOverride[(NoteType) note.type][1];
    }
}