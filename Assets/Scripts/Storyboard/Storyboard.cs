using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cytus2.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard
{
    public class Storyboard
    {
        public List<Text> Texts = new List<Text>();
        public List<Sprite> Sprites = new List<Sprite>();
        public List<Controller> Controllers = new List<Controller>();
        public List<Trigger> Triggers = new List<Trigger>();

        public Dictionary<string, JObject> Templates = new Dictionary<string, JObject>();

        private JObject rootObject;

        public Storyboard(string content)
        {
            rootObject = JObject.Parse(content);
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Templates
            if (rootObject["templates"] != null)
            {
                foreach (var templateProperty in rootObject["templates"].Children<JProperty>())
                {
                    Templates[templateProperty.Name] = templateProperty.Value.ToObject<JObject>();
                }

                // Templates.Keys.ToList().ForEach(key => Debug.Log(JsonConvert.SerializeObject(Templates[key])));
            }

            // Text
            if (rootObject["texts"] != null)
            {
                foreach (var childToken in (JArray) rootObject["texts"])
                {
                    foreach (var objectToken in PopulateJObjects((JObject) childToken))
                    {
                        var text = LoadObject<Text, TextState>(objectToken);
                        if (text != null) Texts.Add(text);
                    }
                }

                // Texts.ForEach(text => Debug.Log(JsonConvert.SerializeObject(text)));
            }

            // Sprite
            if (rootObject["sprites"] != null)
            {
                foreach (var childToken in (JArray) rootObject["sprites"])
                {
                    foreach (var objectToken in PopulateJObjects((JObject) childToken))
                    {
                        var sprite = LoadObject<Sprite, SpriteState>(objectToken);
                        if (sprite != null) Sprites.Add(sprite);
                    }
                }

                // Sprites.ForEach(sprite => Debug.Log(JsonConvert.SerializeObject(sprite)));
            }

            // Controller
            if (rootObject["controllers"] != null)
            {
                foreach (var childToken in (JArray) rootObject["controllers"])
                {
                    foreach (var objectToken in PopulateJObjects((JObject) childToken))
                    {
                        // Controllers must have a time
                        if (objectToken["time"] == null) objectToken["time"] = 0;

                        var controller = LoadObject<Controller, ControllerState>(objectToken);
                        if (controller != null) Controllers.Add(controller);
                    }
                }

                Controllers.ForEach(controller => Debug.Log(JsonConvert.SerializeObject(controller)));
            }

            // Trigger
            if (rootObject["triggers"] != null)
            {
                foreach (var objectToken in (JArray) rootObject["triggers"])
                {
                    Triggers.Add(LoadTrigger(objectToken));
                }

                // Triggers.ForEach(trigger => Debug.Log(JsonConvert.SerializeObject(trigger)));
            }
        }

        public JObject Compile()
        {
            RecursivelyParseTime(rootObject);
            return rootObject;
        }

        private void RecursivelyParseTime(JObject obj)
        {
            foreach (var x in obj)
            {
                var name = x.Key;
                var value = x.Value;
                if (name == "time")
                {
                    value.Replace(ParseTime(value));
                }
                else if (value is JArray)
                {
                    RecursivelyParseTime((JArray) value);
                }
                else if (value is JObject)
                {
                    RecursivelyParseTime((JObject) value);
                }
            }
        }

        private void RecursivelyParseTime(JArray array)
        {
            foreach (var x in array)
            {
                if (x is JArray)
                {
                    RecursivelyParseTime((JArray) x);
                }
                else if (x is JObject)
                {
                    RecursivelyParseTime((JObject) x);
                }
            }
        }

        /**
         * Convert an object with an array of `time` to multiple objects.
         */
        private List<JObject> PopulateJObjects(JObject obj)
        {
            var actualObjects = new List<JObject>();

            var timeToken = obj.SelectToken("relative_time");
            if (timeToken != null && timeToken.Type == JTokenType.Array)
            {
                foreach (var time in timeToken.Values())
                {
                    var newObj = (JObject) obj.DeepClone();
                    newObj["relative_time"] = time;
                    actualObjects.Add(newObj);
                }
            }

            timeToken = obj.SelectToken("add_time");
            if (timeToken != null && timeToken.Type == JTokenType.Array)
            {
                foreach (var time in timeToken.Values())
                {
                    var newObj = (JObject) obj.DeepClone();
                    newObj["add_time"] = time;
                    actualObjects.Add(newObj);
                }
            }

            timeToken = obj.SelectToken("time");
            if (timeToken != null && timeToken.Type == JTokenType.Array)
            {
                foreach (var time in timeToken.Values())
                {
                    var newObj = (JObject) obj.DeepClone();
                    newObj["time"] = time;
                    actualObjects.Add(newObj);
                }
            }

            return actualObjects.Count == 0 ? new List<JObject> {obj} : actualObjects;
        }

        private Trigger LoadTrigger(JToken token)
        {
            var json = token.ToObject<JObject>();
            var trigger = new Trigger();

            trigger.Type = json["type"] != null
                ? (TriggerType) Enum.Parse(typeof(TriggerType), (string) json["type"], true)
                : TriggerType.None;
            trigger.Uses = (int?) json.SelectToken("uses") ?? trigger.Uses;

            trigger.Notes = json["notes"] != null ? json.SelectToken("notes").Values<int>().ToList() : trigger.Notes;
            trigger.Spawn = json["spawn"] != null ? json.SelectToken("spawn").Values<string>().ToList() : trigger.Spawn;
            trigger.Destroy = json["destroy"] != null
                ? json.SelectToken("destroy").Values<string>().ToList()
                : trigger.Destroy;
            trigger.Combo = (int?) json.SelectToken("combo") ?? trigger.Combo;
            trigger.Score = (int?) json.SelectToken("score") ?? trigger.Score;

            return trigger;
        }

        private TO LoadObject<TO, TS>(JToken token) where TO : Object<TS>, new() where TS : ObjectState
        {
            var states = new List<TS>();
            var obj = token.ToObject<JObject>();

            // Create initial state
            var initialState = (TS) CreateState(typeof(TS), (TS) null, obj);
            states.Add(initialState);

            // Create template states
            JToken tmp;
            if (obj.TryGetValue("template", out tmp))
            {
                var templateId = (string) tmp;
                var templateObject = Templates[templateId];

                // Template has states?
                if (templateObject["states"] != null)
                {
                    AddStates(states, initialState, templateObject, ParseTime(obj.SelectToken("time")));
                }
            }

            // Create inline states
            AddStates(states, initialState, obj, ParseTime(obj.SelectToken("time")));

            return new TO
            {
                Id = (string) obj["id"] ?? Path.GetRandomFileName(),
                States = states.OrderBy(state => state.Time).ToList() // Must sort by time
            };
        }

        private void AddStates<T>(List<T> states, T baseState, JObject rootObject, float? rootBaseTime)
            where T : ObjectState
        {
            var baseTime = ParseTime(rootObject.SelectToken("time")) ?? rootBaseTime ?? float.MaxValue;

            if (rootObject["states"] != null && rootObject["states"].Type != JTokenType.Null)
            {
                float lastTime = baseTime;
                
                var allStates = new JArray();
                foreach (var childToken in (JArray) rootObject["states"])
                {
                    var populatedChildren = PopulateJObjects((JObject) childToken);
                    foreach (var child in populatedChildren)
                    {
                        allStates.Add(child);
                    }
                }

                foreach (var stateJson in allStates)
                {
                    var stateObject = stateJson.ToObject<JObject>();
                    var objectState = CreateState(typeof(T), baseState, stateObject);

                    if (objectState.Time != float.MaxValue)
                    {
                        baseTime = objectState.Time;
                    }

                    var relativeTime = (float?) stateObject["relative_time"];

                    if (relativeTime != null)
                    {
                        objectState.RelativeTime = (float) relativeTime;
                        // Use base time + relative time
                        objectState.Time = baseTime + (float) relativeTime;
                    }

                    var addTime = (float?) stateObject["add_time"];

                    if (addTime != null)
                    {
                        objectState.AddTime = (float) addTime;
                        // Use last time + add time
                        objectState.Time = lastTime + (float) addTime;
                    }

                    states.Add((T) objectState);
                    baseState = (T) objectState;

                    lastTime = objectState.Time;

                    // Add inline states
                    if (stateObject["states"] != null)
                    {
                        AddStates(states, baseState, stateObject, rootBaseTime);
                    }
                }
            }
        }

        private ObjectState CreateState<T>(Type type, T baseState, JObject stateObject) where T : ObjectState
        {
            if ((bool?) stateObject["reset"] == true)
            {
                baseState = null; // Allow resetting states
            }

            // Load template
            JObject templateObject = null;
            if (stateObject["template"] != null)
            {
                var templateId = (string) stateObject["template"];
                templateObject = Templates[templateId];

                if (templateObject != null)
                {
                    // Put relative time and add time
                    if (stateObject["relative_time"] == null)
                    {
                        stateObject["relative_time"] = templateObject["relative_time"];
                    }

                    if (stateObject["add_time"] == null)
                    {
                        stateObject["add_time"] = templateObject["add_time"];
                    }

                    // Put template states
                    if (stateObject["states"] == null)
                    {
                        stateObject["states"] = templateObject["states"];
                    }
                }
            }

            if (type == typeof(TextState))
            {
                var state = baseState != null ? (baseState as TextState).Clone() : new TextState();
                if (templateObject != null) ParseTextState(state, templateObject, baseState as TextState);
                ParseTextState(state, stateObject, baseState as TextState);

                return state;
            }

            if (type == typeof(SpriteState))
            {
                var state = baseState != null ? (baseState as SpriteState).Clone() : new SpriteState();
                if (templateObject != null) ParseSpriteState(state, templateObject, baseState as SpriteState);
                ParseSpriteState(state, stateObject, baseState as SpriteState);
                return state;
            }

            if (type == typeof(ControllerState))
            {
                var state = baseState != null ? (baseState as ControllerState).Clone() : new ControllerState();
                if (templateObject != null) ParseControllerState(state, templateObject, baseState as ControllerState);
                ParseControllerState(state, stateObject, baseState as ControllerState);
                return state;
            }

            throw new ArgumentException();
        }

        private float? ParseTime(JToken token)
        {
            if (token == null) return null;

            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                return (float) token;
            }

            if (token.Type == JTokenType.String)
            {
                var split = ((string) token).Split(':');
                var type = split[0].ToLower();
                var offset = 0f;
                if (split.Length == 3)
                {
                    offset = float.Parse(split[2]);
                }

                var id = int.Parse(split[1]);
                switch (type)
                {
                    case "intro":
                        return Game.Instance.Chart.Root.note_list[id].intro_time + offset;
                    case "start":
                        return Game.Instance.Chart.Root.note_list[id].start_time + offset;
                    case "end":
                        return Game.Instance.Chart.Root.note_list[id].end_time + offset;
                }
            }

            return null;
        }

        private void ParseObjectState(ObjectState state, JObject json, ObjectState baseState)
        {
            var token = json.SelectToken("time");
            state.Time = ParseTime(token) ?? state.Time;

            state.Easing = json["easing"] != null
                ? (EasingFunction.Ease) Enum.Parse(typeof(EasingFunction.Ease), (string) json["easing"], true)
                : EasingFunction.Ease.Linear;
            state.Destroy = (bool?) json.SelectToken("destroy") ?? state.Destroy;
        }

        private void ParseSceneObjectState(SceneObjectState state, JObject json, SceneObjectState baseState)
        {
            ParseObjectState(state, json, baseState);

            state.X = (float?) json.SelectToken("x") ?? state.X;
            state.Y = (float?) json.SelectToken("y") ?? state.Y;

            if (baseState != null)
            {
                var baseX = baseState.X;
                if (baseX == float.MinValue) baseX = 0;
                var baseY = baseState.Y;
                if (baseY == float.MinValue) baseY = 0;
                
                var dx = (float?) json.SelectToken("dx");
                var dy = (float?) json.SelectToken("dy");

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

            state.Width = (float?) json.SelectToken("width") ?? state.Width;
            state.Height = (float?) json.SelectToken("height") ?? state.Height;

            state.Layer = (int?) json.SelectToken("layer") ?? state.Layer;
            state.Order = (int?) json.SelectToken("order") ?? state.Order;
        }

        private void ParseTextState(TextState state, JObject json, TextState baseState)
        {
            ParseSceneObjectState(state, json, baseState);

            state.Font = (string) json.SelectToken("font") ?? state.Font;
            UnityEngine.Color tmp;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.Text = (string) json.SelectToken("text") ?? state.Text;
            state.Size = (int?) json.SelectToken("size") ?? state.Size;
            state.Align = (string) json.SelectToken("align") ?? state.Align;
        }

        private void ParseSpriteState(SpriteState state, JObject json, SpriteState baseState)
        {
            ParseSceneObjectState(state, json, baseState);

            state.Path = (string) json.SelectToken("path") ?? state.Path;
            state.PreserveAspect = (bool?) json.SelectToken("preserve_aspect") ?? state.PreserveAspect;
            UnityEngine.Color tmp;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
        }

        private void ParseControllerState(ControllerState state, JObject json, ControllerState baseState)
        {
            ParseObjectState(state, json, baseState);
            UnityEngine.Color tmp;

            state.StoryboardOpacity = (float?) json.SelectToken("storyboard_opacity") ?? state.StoryboardOpacity;
            state.UiOpacity = (float?) json.SelectToken("ui_opacity") ?? state.UiOpacity;
            state.ScanlineOpacity = (float?) json.SelectToken("scanline_opacity") ?? state.ScanlineOpacity;
            state.BackgroundDim = (float?) json.SelectToken("background_dim") ?? state.BackgroundDim;

            state.Size = (float?) json.SelectToken("size") ?? state.Size;
            state.Fov = (float?) json.SelectToken("fov") ?? state.Fov;
            state.Perspective = (bool?) json.SelectToken("perspective") ?? state.Perspective;
            state.X = (float?) json.SelectToken("x") ?? state.X;
            state.Y = (float?) json.SelectToken("y") ?? state.Y;
            state.RotX = (float?) json.SelectToken("rot_x") ?? state.RotX;
            state.RotY = (float?) json.SelectToken("rot_y") ?? state.RotY;
            state.RotZ = (float?) json.SelectToken("rot_z") ?? state.RotZ;

            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("scanline_color"), out tmp))
                state.ScanlineColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.OverrideScanlinePos = (bool?) json.SelectToken("override_scanline_pos") ?? state.OverrideScanlinePos;
            state.ScanlinePos = (float?) json.SelectToken("scanline_pos") ?? state.ScanlinePos;
            state.NoteOpacityMultiplier =
                (float?) json.SelectToken("note_opacity_multiplier") ?? state.NoteOpacityMultiplier;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("note_ring_color"), out tmp))
                state.NoteRingColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            var fillColors = json["note_fill_colors"] != null
                ? json.SelectToken("note_fill_colors").Values<string>().ToList()
                : new List<string>();
            for (var i = 0; i < fillColors.Count; i++)
            {
                var fillColor = fillColors[i];
                if (ColorUtility.TryParseHtmlString(fillColor, out tmp))
                    state.NoteFillColors[i] = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            }

            state.Bloom = (bool?) json.SelectToken("bloom") ?? state.Bloom;
            state.BloomIntensity = (float?) json.SelectToken("bloom_intensity") ?? state.BloomIntensity;

            state.Vignette = (bool?) json.SelectToken("vignette") ?? state.Vignette;
            state.VignetteIntensity = (float?) json.SelectToken("vignette_intensity") ?? state.VignetteIntensity;

            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("vignette_color"), out tmp))
                state.VignetteColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.VignetteStart = (float?) json.SelectToken("vignette_start") ?? state.VignetteStart;
            state.VignetteEnd = (float?) json.SelectToken("vignette_end") ?? state.VignetteEnd;

            state.Chromatic = (bool?) json.SelectToken("chromatic") ?? state.Chromatic;
            state.ChromaticIntensity = (float?) json.SelectToken("chromatic_intensity") ?? state.ChromaticIntensity;
            state.ChromaticStart = (float?) json.SelectToken("chromatic_start") ?? state.ChromaticStart;
            state.ChromaticEnd = (float?) json.SelectToken("chromatic_end") ?? state.ChromaticEnd;

            state.RadialBlur = (bool?) json.SelectToken("radial_blur") ?? state.RadialBlur;
            state.RadialBlurIntensity = (float?) json.SelectToken("radial_blur_intensity") ?? state.RadialBlurIntensity;

            state.ColorAdjustment = (bool?) json.SelectToken("color_adjustment") ?? state.ColorAdjustment;
            state.Brightness = (float?) json.SelectToken("brightness") ?? state.Brightness;
            state.Saturation = (float?) json.SelectToken("saturation") ?? state.Saturation;
            state.Contrast = (float?) json.SelectToken("contrast") ?? state.Contrast;

            state.ColorFilter = (bool?) json.SelectToken("color_filter") ?? state.ColorFilter;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color_filter_color"), out tmp))
                state.ColorFilterColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};

            state.GrayScale = (bool?) json.SelectToken("gray_scale") ?? state.GrayScale;
            state.GrayScaleIntensity = (float?) json.SelectToken("gray_scale_intensity") ?? state.GrayScaleIntensity;

            state.Noise = (bool?) json.SelectToken("noise") ?? state.Noise;
            state.NoiseIntensity = (float?) json.SelectToken("noise_intensity") ?? state.NoiseIntensity;

            state.Sepia = (bool?) json.SelectToken("sepia") ?? state.Sepia;
            state.SepiaIntensity = (float?) json.SelectToken("sepia_intensity") ?? state.SepiaIntensity;

            state.Dream = (bool?) json.SelectToken("dream") ?? state.Dream;
            state.DreamIntensity = (float?) json.SelectToken("dream_intensity") ?? state.DreamIntensity;

            state.Fisheye = (bool?) json.SelectToken("fisheye") ?? state.Fisheye;
            state.FisheyeIntensity = (float?) json.SelectToken("fisheye_intensity") ?? state.FisheyeIntensity;

            state.Shockwave = (bool?) json.SelectToken("shockwave") ?? state.Shockwave;
            state.ShockwaveSpeed = (float?) json.SelectToken("shockwave_speed") ?? state.ShockwaveSpeed;

            state.Focus = (bool?) json.SelectToken("focus") ?? state.Focus;
            state.FocusIntensity = (float?) json.SelectToken("focus_intensity") ?? state.FocusIntensity;
            state.FocusSize = (float?) json.SelectToken("focus_size") ?? state.FocusSize;
            state.FocusSpeed = (float?) json.SelectToken("focus_speed") ?? state.FocusSpeed;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("focus_color"), out tmp))
                state.FocusColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};

            state.Glitch = (bool?) json.SelectToken("glitch") ?? state.Glitch;
            state.GlitchIntensity = (float?) json.SelectToken("glitch_intensity") ?? state.GlitchIntensity;

            state.Artifact = (bool?) json.SelectToken("artifact") ?? state.Artifact;
            state.ArtifactIntensity = (float?) json.SelectToken("artifact_intensity") ?? state.ArtifactIntensity;
            state.ArtifactColorisation =
                (float?) json.SelectToken("artifact_colorisation") ?? state.ArtifactColorisation;
            state.ArtifactParasite = (float?) json.SelectToken("artifact_parasite") ?? state.ArtifactParasite;
            state.ArtifactNoise = (float?) json.SelectToken("artifact_noise") ?? state.ArtifactNoise;

            state.Arcade = (bool?) json.SelectToken("arcade") ?? state.Arcade;
            state.ArcadeIntensity = (float?) json.SelectToken("arcade_intensity") ?? state.ArcadeIntensity;
            state.ArcadeInterferanceSize =
                (float?) json.SelectToken("arcade_interferance_size") ?? state.ArcadeInterferanceSize;
            state.ArcadeInterferanceSpeed =
                (float?) json.SelectToken("arcade_interferance_speed") ?? state.ArcadeInterferanceSpeed;
            state.ArcadeContrast = (float?) json.SelectToken("arcade_contrast") ?? state.ArcadeContrast;

            state.Chromatical = (bool?) json.SelectToken("chromatical") ?? state.Chromatical;
            state.ChromaticalFade = (float?) json.SelectToken("chromatical_fade") ?? state.ChromaticalFade;
            state.ChromaticalIntensity =
                (float?) json.SelectToken("chromatical_intensity") ?? state.ChromaticalIntensity;
            state.ChromaticalSpeed = (float?) json.SelectToken("chromatical_speed") ?? state.ChromaticalSpeed;

            state.Tape = (bool?) json.SelectToken("tape") ?? state.Tape;
        }
    }
}