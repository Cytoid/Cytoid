using System;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig
{
    public float ChartOffset;
    public float NoteHitboxMultiplier;
    public bool UseScannerSmoothing;
    
    public float GlobalOpacityMultiplier = 1f;
    public Color GlobalRingColorOverride;
    public readonly Dictionary<NoteType, Color[]> GlobalFillColorsOverride = new Dictionary<NoteType, Color[]>();

    public float ChartNoteSizeMultiplier;
    public float PlayerNoteSizeOffset;
    public readonly Dictionary<NoteType, float> NoteSizes = new Dictionary<NoteType, float>();
    public readonly Dictionary<NoteType, Color[]> NoteRingColors = new Dictionary<NoteType, Color[]>();
    public readonly Dictionary<NoteType, Color[]> NoteFillColors = new Dictionary<NoteType, Color[]>();
    public Dictionary<NoteGrade, Color> NoteGradeEffectColors = new Dictionary<NoteGrade, Color>();

    public GameConfig(Game game)
    {
        game.onGameLoaded.AddListener(OnGameLoaded);
    }

    private static readonly Dictionary<NoteType, int[]> NoteColorChartOverrideMapping = new Dictionary<NoteType, int[]>
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

        ChartOffset = Context.LocalPlayer.MainChartOffset + Context.LocalPlayer.GetLevelChartOffset(game.Level.Meta.id);
        if (DetectHeadset.Detect()) ChartOffset += Context.LocalPlayer.HeadsetOffset;
        
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

        PlayerNoteSizeOffset = ((int) Context.LocalPlayer.NoteSize - 3) * 0.1f;
        ChartNoteSizeMultiplier = (float) chart.Model.size * (1 + PlayerNoteSizeOffset);

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
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][0]]?.ToColor() ?? Context.LocalPlayer.GetFillColor(type, false),
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][1]]?.ToColor() ?? Context.LocalPlayer.GetFillColor(type, true)
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
        return alt ? GlobalFillColorsOverride[(NoteType) note.type][0] : GlobalFillColorsOverride[(NoteType) note.type][1];
    }
}