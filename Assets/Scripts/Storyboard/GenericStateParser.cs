using System;
using Newtonsoft.Json.Linq;

namespace Cytoid.Storyboard
{
    public abstract class StateParser
    {
        public abstract void Parse(ObjectState state, JObject json, ObjectState baseState);
    }
    
    public abstract class GenericStateParser<TS> : StateParser where TS : ObjectState 
    {
        public Storyboard Storyboard { get; }

        public GenericStateParser(Storyboard storyboard)
        {
            Storyboard = storyboard;
        }

        public abstract void Parse(TS state, JObject json, TS baseState);

        public override void Parse(ObjectState state, JObject json, ObjectState baseState)
        {
            Parse((TS) state, json, (TS) baseState);
        }
        
        protected void ParseObjectState(ObjectState state, JObject json, ObjectState baseState)
        {
            var token = json.SelectToken("time");
            state.Time = Storyboard.ParseTime(json, token) ?? state.Time;

            state.Easing = json["easing"] != null
                ? (EasingFunction.Ease) Enum.Parse(typeof(EasingFunction.Ease), (string) json["easing"], true)
                : EasingFunction.Ease.Linear;
            state.Destroy = (bool?) json.SelectToken("destroy") ?? state.Destroy;
        }
        
        protected void ParseCanvasObjectState(CanvasObjectState state, JObject json, CanvasObjectState baseState)
        {
            ParseObjectState(state, json, baseState);

            state.X = ParseNumber(json.SelectToken("x"), ReferenceUnit.StageX, true, false) ?? state.X;
            state.Y = ParseNumber(json.SelectToken("y"), ReferenceUnit.StageY, true, false) ?? state.Y;
            state.Z = ParseNumber(json.SelectToken("z"), ReferenceUnit.World, true, false) ?? state.Z;

            if (baseState != null)
            {
                var baseX = baseState.X;
                if (!baseX.IsSet()) baseX = 0;
                var baseY = baseState.Y;
                if (!baseY.IsSet()) baseY = 0;

                var dx = ParseNumber(json.SelectToken("dx"), ReferenceUnit.StageX, true, true);
                var dy = ParseNumber(json.SelectToken("dy"), ReferenceUnit.StageY, true, true);

                if (dx != null) state.X = baseX + (float) dx;
                if (dy != null) state.Y = baseY + (float) dy;
            }

            state.RotX = (float?) json.SelectToken("rot_x") ?? state.RotX;
            state.RotY = (float?) json.SelectToken("rot_y") ?? state.RotY;
            state.RotZ = (float?) json.SelectToken("rot_z") ?? state.RotZ;

            state.ScaleX = (float?) json.SelectToken("scale_x") ?? state.ScaleX;
            state.ScaleY = (float?) json.SelectToken("scale_y") ?? state.ScaleY;
            if (json["scale"] != null)
            {
                var scale = (float) json.SelectToken("scale");
                state.ScaleX = scale;
                state.ScaleY = scale;
            }

            state.Opacity = (float?) json.SelectToken("opacity") ?? state.Opacity;

            state.Width = ParseNumber(json.SelectToken("width"), ReferenceUnit.StageX, true, true) ?? state.Width;
            state.Height = ParseNumber(json.SelectToken("height"), ReferenceUnit.StageY, true, true) ?? state.Height;
            state.FillWidth = (bool?) json.SelectToken("fill_width") ?? state.FillWidth;

            state.Layer = (int?) json.SelectToken("layer") ?? state.Layer;
            state.Order = (int?) json.SelectToken("order") ?? state.Order;
        }
        
        protected float? ParseNumber(JToken token, ReferenceUnit defaultUnit, bool scaleToCanvas, bool span)
        {
            if (token == null) return null;
            if (token.Type == JTokenType.Integer) return ConvertNumber((int) token, defaultUnit);
            if (token.Type == JTokenType.Float) return ConvertNumber((float) token, defaultUnit);
            if (token.Type == JTokenType.String)
            {
                var split = ((string) token).Split(':');
                if (split.Length == 1) return ConvertNumber(float.Parse(split[0]), defaultUnit);
                var type = split[0].ToLower();
                var value = float.Parse(split[1]);
                return ConvertNumber(value, (ReferenceUnit) Enum.Parse(typeof(ReferenceUnit), type, true));
            }

            float ConvertNumber(float value, ReferenceUnit unit)
            {
                float res;
                switch (unit)
                {
                    case ReferenceUnit.World:
                        res = value;
                        break;
                    case ReferenceUnit.StageX:
                        res = value / StoryboardRenderer.ReferenceWidth * Storyboard.Game.camera.orthographicSize / UnityEngine.Screen.height * UnityEngine.Screen.width;
                        break;
                    case ReferenceUnit.StageY:
                        res = value / StoryboardRenderer.ReferenceHeight * Storyboard.Game.camera.orthographicSize;
                        break;
                    case ReferenceUnit.NoteX:
                        res = Storyboard.Game.Chart.Let(it => it.ConvertChartXToScreenX(value) - (span ? it.ConvertChartXToScreenX(0) : 0));
                        break;
                    case ReferenceUnit.NoteY:
                        res = Storyboard.Game.Chart.Let(it => it.ConvertChartYToScreenY(value) - (span ? it.ConvertChartYToScreenY(0) : 0));
                        break;
                    case ReferenceUnit.CameraX:
                        res = value * Storyboard.Game.camera.orthographicSize / UnityEngine.Screen.height * UnityEngine.Screen.width;
                        break;
                    case ReferenceUnit.CameraY:
                        res = value * Storyboard.Game.camera.orthographicSize;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (scaleToCanvas)
                {
                    switch (unit)
                    {
                        case ReferenceUnit.StageX:
                        case ReferenceUnit.NoteX:
                        case ReferenceUnit.CameraX:
                            res = res / (Storyboard.Game.camera.orthographicSize / UnityEngine.Screen.height * UnityEngine.Screen.width) * Storyboard.Renderer.Provider.CanvasRect.width;
                            break;
                        case ReferenceUnit.StageY:
                        case ReferenceUnit.NoteY:
                        case ReferenceUnit.CameraY:
                            res = res / Storyboard.Game.camera.orthographicSize * Storyboard.Renderer.Provider.CanvasRect.height;
                            break;
                    }
                }
                return res;
            }
            throw new ArgumentException();
        }
    }
    
    public enum ReferenceUnit
    {
        World,
        StageX, StageY, // Canvas: 800 x 600 
        NoteX, NoteY, // Notes: 1 x 1
        CameraX, CameraY, // Orthographic
    }
}