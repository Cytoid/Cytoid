using System;
using System.Collections.Generic;
using Cytoid.Storyboard;
using Cytus2.Controllers;
using Cytus2.Views;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Cytus2.Models
{
    public class SimpleVisualOptions : SingletonMonoBehavior<SimpleVisualOptions>
    {
       
        public float GlobalOpacityMultiplier = 1f;
        public Color GlobalRingColorOverride = Color.clear;
        public Color[] GlobalFillColorsOverride = new Color[10];
        
        private float userNoteSizeOffset;
        
        protected override void Awake()
        {
            EventKit.Subscribe("game loaded", OnGameLoaded);
            for (var i = 0; i < 10; i++) GlobalFillColorsOverride[i] = Color.clear;
        }

        public void OnGameLoaded()
        {
            var chart = Game.Instance.Chart.Root;
            
            userNoteSizeOffset = ((int) PlayerPrefs.GetFloat("note size", 3) - 3) * 0.1f;
            SizeMultiplier = (float) chart.size * (1 + userNoteSizeOffset);

            ClickSize = Camera.main.orthographicSize * 2.0f * 7.0f / 9.0f / 5.0f * 1.2675f;
            DragHeadSize = ClickSize * 0.8f;
            DragChildSize = ClickSize * 0.65f;
            HoldSize = ClickSize;
            LongHoldSize = ClickSize;
            FlickSize = ClickSize * 1.125f;

            if (!(Game.Instance is StoryboardGame))
            {
                var color = Convert.HexToColor(PlayerPrefs.GetString("ring color"));
                if (chart.ring_color != null) color = Convert.HexToColor(chart.ring_color);
                RingColorClick1 = color;
                RingColorClick2 = color;
                RingColorDrag1 = color;
                RingColorDrag2 = color;
                RingColorHold1 = color;
                RingColorHold2 = color;
                RingColorLongHold1 = color;
                RingColorLongHold2 = color;
                RingColorFlick1 = color;
                RingColorFlick2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (click 1)"));
                if (chart.fill_colors[0] != null)
                    color = Convert.HexToColor(chart.fill_colors[0]);
                FillColorClick1 = color;
                
                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (click 2)"));
                if (chart.fill_colors[1] != null)
                    color = Convert.HexToColor(chart.fill_colors[1]);
                FillColorClick2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (drag 1)"));
                if (chart.fill_colors[2] != null)
                    color = Convert.HexToColor(chart.fill_colors[2]);
                FillColorDrag1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (drag 2)"));
                if (chart.fill_colors[3] != null)
                    color = Convert.HexToColor(chart.fill_colors[3]);
                FillColorDrag2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (hold 1)"));
                if (chart.fill_colors[4] != null)
                    color = Convert.HexToColor(chart.fill_colors[4]);
                FillColorHold1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (hold 2)"));
                if (chart.fill_colors[5] != null)
                    color = Convert.HexToColor(chart.fill_colors[5]);
                FillColorHold2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (long hold 1)"));
                if (chart.fill_colors[6] != null)
                    color = Convert.HexToColor(chart.fill_colors[6]);
                FillColorLongHold1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (long hold 2)"));
                if (chart.fill_colors[7] != null)
                    color = Convert.HexToColor(chart.fill_colors[7]);
                FillColorLongHold2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (flick 1)"));
                if (chart.fill_colors[8] != null)
                    color = Convert.HexToColor(chart.fill_colors[8]);
                FillColorFlick1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (flick 2)"));
                if (chart.fill_colors[9] != null)
                    color = Convert.HexToColor(chart.fill_colors[9]);
                FillColorFlick2 = color;
            }
        }
        
        public float SizeMultiplier = 1;

        [HideInInspector] public float ClickSize;
        [HideInInspector] public float DragHeadSize;
        [HideInInspector] public float DragChildSize;
        [HideInInspector] public float HoldSize;
        [HideInInspector] public float LongHoldSize;
        [HideInInspector] public float FlickSize;

        public Color PerfectColor;
        public Color GreatColor;
        public Color GoodColor;
        public Color BadColor;
        public Color MissColor;

        public Color RingColorClick1;
        public Color FillColorClick1;

        public Color RingColorClick2;
        public Color FillColorClick2;

        public Color RingColorDrag1;
        public Color FillColorDrag1;

        public Color RingColorDrag2;
        public Color FillColorDrag2;

        public Color RingColorHold1;
        public Color FillColorHold1;

        public Color RingColorHold2;
        public Color FillColorHold2;

        public Color RingColorLongHold1;
        public Color FillColorLongHold1;

        public Color RingColorLongHold2;
        public Color FillColorLongHold2;

        public Color RingColorFlick1;
        public Color FillColorFlick1;

        public Color RingColorFlick2;
        public Color FillColorFlick2;

        public float GetSize(NoteView noteView)
        {
            var multiplier = SizeMultiplier;
            if (noteView.Note.Note.size != double.MinValue)
            {
                multiplier = (float) noteView.Note.Note.size * (1 + userNoteSizeOffset);
            }

            if (noteView is ClickNoteView) return ClickSize * multiplier;
            if (noteView is DragHeadNoteView) return DragHeadSize * multiplier;
            if (noteView is DragChildNoteView) return DragChildSize * multiplier;
            if (noteView is LongHoldNoteView) return LongHoldSize * multiplier;
            if (noteView is HoldNoteView) return HoldSize * multiplier;
            if (noteView is FlickNoteView) return FlickSize * multiplier;

            throw new NotImplementedException();
        }

        public Color GetRingColor(NoteView noteView)
        {
            if (GlobalRingColorOverride != Color.clear)
            {
                return GlobalRingColorOverride;
            }
            
            if (noteView.Note.Note.ring_color != null)
            {
                return noteView.Note.Note.ParsedRingColor;
            }

            var alt = noteView.Note.Note.direction > 0;
            if (noteView is ClickNoteView) return alt ? RingColorClick1 : RingColorClick2;
            if (noteView is DragHeadNoteView) return alt ? RingColorDrag1 : RingColorDrag2;
            if (noteView is DragChildNoteView) return alt ? RingColorDrag1 : RingColorDrag2;
            if (noteView is LongHoldNoteView) return alt ? RingColorLongHold1 : RingColorLongHold2;
            if (noteView is HoldNoteView) return alt ? RingColorHold1 : RingColorHold2;
            if (noteView is FlickNoteView) return alt ? RingColorFlick1 : RingColorFlick2;

            throw new NotImplementedException();
        }

        public Color GetFillColor(NoteView noteView)
        {
            // Special case
            if (noteView is DragChildNoteView) return GetRingColor(noteView);
            
            var alt = noteView.Note.Note.direction > 0;
            if (noteView.Note.Note.is_forward) alt = !alt;
            
            var i = 0;
            if (noteView is ClickNoteView) i = alt ? 1 : 0;
            else if (noteView is DragHeadNoteView) i = alt ? 3 : 2;
            else if (noteView is LongHoldNoteView) i = alt ? 7 : 6;
            else if (noteView is HoldNoteView) i = alt ? 5 : 4;
            else if (noteView is FlickNoteView) i = alt ? 9 : 8;
            if (GlobalFillColorsOverride[i] != Color.clear)
            {
                return GlobalFillColorsOverride[i];
            }
            
            if (noteView.Note.Note.fill_color != null)
            {
                return noteView.Note.Note.ParsedFillColor;
            }
            
            if (noteView is ClickNoteView) return alt ? FillColorClick1 : FillColorClick2;
            if (noteView is DragHeadNoteView) return alt ? FillColorDrag1 : FillColorDrag2;
            if (noteView is LongHoldNoteView) return alt ? FillColorLongHold1 : FillColorLongHold2;
            if (noteView is HoldNoteView) return alt ? FillColorHold1 : FillColorHold2;
            if (noteView is FlickNoteView) return alt ? FillColorFlick1 : FillColorFlick2;

            throw new NotImplementedException();
        }
    }
}