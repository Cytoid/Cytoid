using System;
using Cytoid.Storyboard;
using Cytus2.Controllers;
using Cytus2.Views;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Cytus2.Models
{
    public class SimpleVisualOptions : SingletonMonoBehavior<SimpleVisualOptions>
    {
        protected override void Awake()
        {
            ClickSize = Camera.main.orthographicSize * 2.0f * 7.0f / 9.0f / 5.0f * 1.2675f;
            DragHeadSize = ClickSize * 0.8f;
            DragChildSize = ClickSize * 0.65f;
            HoldSize = ClickSize;
            LongHoldSize = ClickSize;
            FlickSize = ClickSize * 1.125f;

            if (!(Game.Instance is StoryboardGame))
            {
                var color = Convert.HexToColor(PlayerPrefs.GetString("ring color"));
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
                FillColorClick1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (click 2)"));
                FillColorClick2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (drag 1)"));
                FillColorDrag1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (drag 2)"));
                FillColorDrag2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (hold 1)"));
                FillColorHold1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (hold 2)"));
                FillColorHold2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (long hold 1)"));
                FillColorLongHold1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (long hold 2)"));
                FillColorLongHold2 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (flick 1)"));
                FillColorFlick1 = color;

                color = Convert.HexToColor(PlayerPrefs.GetString("fill color (flick 2)"));
                FillColorFlick2 = color;
            }
        }

        public bool OpaqueDragLine = true;

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
            if (noteView is ClickNoteView) return ClickSize;
            if (noteView is DragHeadNoteView) return DragHeadSize;
            if (noteView is DragChildNoteView) return DragChildSize;
            if (noteView is LongHoldNoteView) return LongHoldSize;
            if (noteView is HoldNoteView) return HoldSize;
            if (noteView is FlickNoteView) return FlickSize;

            throw new NotImplementedException();
        }

        public Color GetRingColor(NoteView noteView)
        {
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
            var alt = noteView.Note.Note.direction > 0;
            if (noteView is ClickNoteView) return alt ? FillColorClick1 : FillColorClick2;
            if (noteView is DragHeadNoteView) return alt ? FillColorDrag1 : FillColorDrag2;
            if (noteView is DragChildNoteView) return alt ? RingColorDrag1 : RingColorDrag2;
            if (noteView is LongHoldNoteView) return alt ? FillColorLongHold1 : FillColorLongHold2;
            if (noteView is HoldNoteView) return alt ? FillColorHold1 : FillColorHold2;
            if (noteView is FlickNoteView) return alt ? FillColorFlick1 : FillColorFlick2;

            throw new NotImplementedException();
        }
    }
}