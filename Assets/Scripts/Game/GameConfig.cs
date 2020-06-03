using System;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig
{
    public float ChartOffset;
    public bool UseScannerSmoothing;

    public float GlobalNoteSizeMultiplier;
    public float GlobalNoteOpacityMultiplier = 1f;
    // TODO: Optimize color override; don't compare it with Color.clear!
    public Color GlobalRingColorOverride;
    public readonly Dictionary<NoteType, Color[]> GlobalFillColorsOverride = new Dictionary<NoteType, Color[]>();

    public bool UseClassicStyle = true;
    public bool DisplayNoteIds;
    public readonly Dictionary<NoteType, float> NoteSizes = new Dictionary<NoteType, float>();
    public readonly Dictionary<NoteType, float> NoteHitboxSizes = new Dictionary<NoteType, float>();
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
        {NoteType.Flick, new[] {8, 9}},
        {NoteType.CDragHead, new[] {10, 11}},
        {NoteType.CDragChild, new[] {10, 11}}
    };

    public void OnGameLoaded(Game game)
    {
        var chart = game.Chart;

        var lp = Context.Player;
        ChartOffset = lp.Settings.BaseNoteOffset + game.Level.Record.RelativeNoteOffset;
        if (DetectHeadset.Detect()) ChartOffset += lp.Settings.HeadsetNoteOffset;

        UseScannerSmoothing = true;
        DisplayNoteIds = Context.Player.Settings.DisplayNoteIds;

        GlobalRingColorOverride = chart.Model.ring_color?.ToColor() ?? Color.clear;
        foreach (NoteType type in Enum.GetValues(typeof(NoteType)))
        {
            GlobalFillColorsOverride[type] = new[]
            {
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][0]]?.ToColor() ?? Color.clear,
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][1]]?.ToColor() ?? Color.clear
            };
        }

        GlobalNoteSizeMultiplier = (float) chart.Model.size * (1 + 0.133333f + lp.Settings.NoteSize);

        NoteSizes[NoteType.Click] = NoteSizes[NoteType.CDragHead] = (game.camera.orthographicSize * 2.0f) * (7.0f / 9.0f) / 5.0f * 1.2675f;
        NoteSizes[NoteType.DragHead] = NoteSizes[NoteType.Click] * 0.8f;
        NoteSizes[NoteType.DragChild] = NoteSizes[NoteType.CDragChild] = NoteSizes[NoteType.Click] * 0.65f;
        NoteSizes[NoteType.Hold] = NoteSizes[NoteType.Click];
        NoteSizes[NoteType.LongHold] = NoteSizes[NoteType.Click];
        NoteSizes[NoteType.Flick] = NoteSizes[NoteType.Click] * 1.125f;

        NoteHitboxSizes[NoteType.Click] = NoteHitboxSizes[NoteType.CDragHead] =
            new[] {0.666666f * 1.111111f, 0.666666f * 1.333333f, 0.666666f * 1.555555f}[lp.Settings.HitboxSizes[NoteType.Click]];
        NoteHitboxSizes[NoteType.DragHead] =
            new[] {0.666666f * 1.111111f, 0.666666f * 1.333333f, 0.666666f * 1.555555f}[lp.Settings.HitboxSizes[NoteType.DragChild]];
        NoteHitboxSizes[NoteType.DragChild] = NoteHitboxSizes[NoteType.CDragChild] =
            new[] {0.888888f * 1.111111f, 0.888888f * 1.333333f, 0.888888f * 1.555555f}[lp.Settings.HitboxSizes[NoteType.DragChild]];
        NoteHitboxSizes[NoteType.Hold] = NoteHitboxSizes[NoteType.LongHold] =
            new[] {0.888888f * 1.111111f, 0.888888f * 1.333333f, 0.888888f * 1.555555f}[lp.Settings.HitboxSizes[NoteType.Hold]];
        NoteHitboxSizes[NoteType.Flick] =
            new[] {0.888888f * 1.111111f, 0.888888f * 1.333333f, 0.888888f * 1.555555f}[lp.Settings.HitboxSizes[NoteType.Flick]];

        foreach (NoteType type in Enum.GetValues(typeof(NoteType)))
        {
            var eqType = type;
            if (type == NoteType.DragHead) eqType = NoteType.DragChild;
            if (type == NoteType.CDragHead) eqType = NoteType.CDragChild;
            NoteRingColors[type] = new[]
            {
                chart.Model.ring_color?.ToColor() ?? lp.Settings.NoteRingColors[NoteType.Click],
                chart.Model.ring_color?.ToColor() ?? lp.Settings.NoteRingColors[NoteType.Click]
            };
            NoteFillColors[type] = new[]
            {
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][0]]?.ToColor() ??
                lp.Settings.NoteFillColors[eqType],
                chart.Model.fill_colors[NoteColorChartOverrideMapping[type][1]]?.ToColor() ??
                lp.Settings.NoteFillColorsAlt[eqType],
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

        if (game.Storyboard != null && game.Storyboard.Controllers.Count > 0)
        {
            game.Storyboard.Config.UseEffects = Context.Player.Settings.DisplayStoryboardEffects;
        }

        Context.UpdateGraphicsQuality();
    }

    public Color GetRingColor(ChartModel.Note note)
    {
        return note.UseAlternativeColor() ? NoteRingColors[(NoteType) note.type][0] : NoteRingColors[(NoteType) note.type][1];
    }

    public Color GetFillColor(ChartModel.Note note)
    {
        if (!Context.Player.Settings.UseFillColorForDragChildNodes && 
            ((NoteType) note.type == NoteType.DragChild || (NoteType) note.type == NoteType.CDragChild)) return GetRingColor(note);
        return note.UseAlternativeColor() ? NoteFillColors[(NoteType) note.type][0] : NoteFillColors[(NoteType) note.type][1];
    }

    public Color GetRingColorOverride(ChartModel.Note note)
    {
        if (note.Override.RingColor != null) return note.Override.RingColor.Value;
        return GlobalRingColorOverride;
    }

    public Color GetFillColorOverride(ChartModel.Note note)
    {
        if (note.Override.FillColor != null) return note.Override.FillColor.Value;
        return note.UseAlternativeColor()
            ? GlobalFillColorsOverride[(NoteType) note.type][0]
            : GlobalFillColorsOverride[(NoteType) note.type][1];
    }
}