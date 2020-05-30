using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

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
        
        protected void ParseStageObjectState(StageObjectState state, JObject json, StageObjectState baseState)
        {
            ParseObjectState(state, json, baseState);

            state.X = ParseUnitFloat(json.SelectToken("x"), ReferenceUnit.StageX, true, false) ?? state.X;
            state.Y = ParseUnitFloat(json.SelectToken("y"), ReferenceUnit.StageY, true, false) ?? state.Y;
            state.Z = ParseUnitFloat(json.SelectToken("z"), ReferenceUnit.World, true, false) ?? state.Z;

            if (baseState != null)
            {
                var baseX = baseState.X?.Value ?? 0;
                var baseY = baseState.Y?.Value ?? 0;

                var dx = ParseUnitFloat(json.SelectToken("dx"), ReferenceUnit.StageX, true, true);
                var dy = ParseUnitFloat(json.SelectToken("dy"), ReferenceUnit.StageY, true, true);

                if (dx != null) state.X = dx.WithValue(dx.Value + baseX);
                if (dy != null) state.Y = dy.WithValue(dy.Value + baseY);
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

            state.Width = ParseUnitFloat(json.SelectToken("width"), ReferenceUnit.StageX, true, true) ?? state.Width;
            state.Height = ParseUnitFloat(json.SelectToken("height"), ReferenceUnit.StageY, true, true) ?? state.Height;
            state.FillWidth = (bool?) json.SelectToken("fill_width") ?? state.FillWidth;

            state.Layer = (int?) json.SelectToken("layer") ?? state.Layer;
            state.Order = (int?) json.SelectToken("order") ?? state.Order;
        }
        
        protected UnitFloat ParseUnitFloat(JToken token, ReferenceUnit defaultUnit, bool scaleToCanvas, bool span, float? defaultValue = null)
        {
            if (token == null)
            {
                if (defaultValue == null) return null;
                return new UnitFloat(defaultValue.Value, defaultUnit, scaleToCanvas, span);
            }
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return new UnitFloat((int) token, defaultUnit, scaleToCanvas, span);
                case JTokenType.Float:
                    return new UnitFloat((float) token, defaultUnit, scaleToCanvas, span);
                case JTokenType.String:
                {
                    var split = ((string) token).Split(':');
                    if (split.Length == 1) return new UnitFloat(float.Parse(split[0]), defaultUnit, scaleToCanvas, span);
                    var type = split[0].ToLower();
                    var value = float.Parse(split[1]);
                    return new UnitFloat(value, (ReferenceUnit) Enum.Parse(typeof(ReferenceUnit), type, true), scaleToCanvas, span);
                }
                default:
                    throw new ArgumentException();
            }
        }
    }
    
    public enum ReferenceUnit
    {
        World,
        StageX, StageY, // Canvas: 800 x 600 
        NoteX, NoteY, // Notes: 1 x 1
        CameraX, CameraY, // Orthographic
    }

    [Serializable]
    public class UnitFloat
    {
        [JsonIgnore] public static Storyboard Storyboard;
        
        public float Value;
        public ReferenceUnit Unit;
        public bool ScaleToCanvas;
        public bool Span;

        [JsonIgnore]
        public float ConvertedValue
        {
            get
            {
                if (convertedValue != null) return convertedValue.Value;
                convertedValue = Convert();
                return convertedValue.Value;
            }
        }

        [JsonIgnore] private float? convertedValue;

        public UnitFloat(float value, ReferenceUnit unit, bool scaleToCanvas, bool span)
        {
            Value = value;
            Unit = unit;
            ScaleToCanvas = scaleToCanvas;
            Span = span;
        }

        public UnitFloat WithValue(float value)
        {
            return new UnitFloat(value, Unit, ScaleToCanvas, Span);
        }

        public float Convert()
        {
            float res;
            switch (Unit)
            {
                case ReferenceUnit.World:
                    res = Value;
                    break;
                case ReferenceUnit.StageX:
                    res = Value / StoryboardRenderer.ReferenceWidth * Storyboard.Game.camera.orthographicSize /
                        UnityEngine.Screen.height * UnityEngine.Screen.width;
                    break;
                case ReferenceUnit.StageY:
                    res = Value / StoryboardRenderer.ReferenceHeight * Storyboard.Game.camera.orthographicSize;
                    break;
                case ReferenceUnit.NoteX:
                    res = Storyboard.Game.Chart.Let(it =>
                        it.ConvertChartXToScreenX(Value) - (Span ? it.ConvertChartXToScreenX(0) : 0));
                    break;
                case ReferenceUnit.NoteY:
                    res = Storyboard.Game.Chart.Let(it =>
                        it.ConvertChartYToScreenY(Value) - (Span ? it.ConvertChartYToScreenY(0) : 0));
                    break;
                case ReferenceUnit.CameraX:
                    res = Value * Storyboard.Game.camera.orthographicSize / UnityEngine.Screen.height *
                          UnityEngine.Screen.width;
                    break;
                case ReferenceUnit.CameraY:
                    res = Value * Storyboard.Game.camera.orthographicSize;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ScaleToCanvas)
            {
                switch (Unit)
                {
                    case ReferenceUnit.NoteX:
                        res = res / (Storyboard.Game.camera.orthographicSize * 2 / UnityEngine.Screen.height *
                                     UnityEngine.Screen.width) * Storyboard.Renderer.Provider.CanvasRect.width;
                        break;
                    case ReferenceUnit.StageX:
                    case ReferenceUnit.CameraX:
                        res = res / (Storyboard.Game.camera.orthographicSize / UnityEngine.Screen.height *
                                     UnityEngine.Screen.width) * Storyboard.Renderer.Provider.CanvasRect.width;
                        break;
                    case ReferenceUnit.NoteY:
                        res = res / (Storyboard.Game.camera.orthographicSize * 2) *
                              Storyboard.Renderer.Provider.CanvasRect.height;
                        break;
                    case ReferenceUnit.StageY:
                    case ReferenceUnit.CameraY:
                        res = res / Storyboard.Game.camera.orthographicSize *
                              Storyboard.Renderer.Provider.CanvasRect.height;
                        break;
                }
            }
            return res;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        
    }

}