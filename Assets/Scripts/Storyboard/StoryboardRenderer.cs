using System;
using System.Collections.Generic;
using System.Linq;
using Cytoid.Storyboard.Controllers;
using Cytoid.Storyboard.Sprites;
using Cytoid.Storyboard.Texts;
using Cytoid.Storyboard.Videos;
using UniRx.Async;
using UnityEngine;
using LineRenderer = Cytoid.Storyboard.Sprites.LineRenderer;
using SpriteRenderer = Cytoid.Storyboard.Sprites.SpriteRenderer;

namespace Cytoid.Storyboard
{
    public class StoryboardRenderer
    {
        public const int ReferenceWidth = 800;
        public const int ReferenceHeight = 600;

        public Storyboard Storyboard { get; }
        public Game Game => Storyboard.Game;
        public float Time => Game.Time;
        public StoryboardRendererProvider Provider => StoryboardRendererProvider.Instance;
        
        public readonly Dictionary<string, List<StoryboardComponentRenderer>> ComponentRenderers = 
            new Dictionary<string, List<StoryboardComponentRenderer>>(); // Object ID to multiple renderer instances

        public StoryboardRenderer(Storyboard storyboard)
        {
            Storyboard = storyboard;
        }

        public void Clear()
        {
            ComponentRenderers.Values.ForEach(renderers =>
            {
                renderers.ForEach(it => it.Clear());
            });

            ResetCamera();
            ResetCameraFilters();
        }

        private void ResetCamera()
        {
            var camera = Provider.Camera;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.transform.eulerAngles = Vector3.zero;
            camera.orthographic = true;
            camera.fieldOfView = 53.2f;
        }

        private void ResetCameraFilters()
        {
            Provider.RadialBlur.Apply(it =>
            {
                it.enabled = false;
                it.Intensity = 0.025f;
            });
            Provider.ColorAdjustment.Apply(it =>
            {
                it.enabled = false;
                it.Brightness = 1;
                it.Saturation = 1;
                it.Contrast = 1;
            });
            Provider.GrayScale.Apply(it =>
            {
                it.enabled = false;
                it._Fade = 1;
            });
            Provider.Noise.Apply(it =>
            {
                it.enabled = false;
                it.Noise = 0.2f;
            });
            Provider.ColorFilter.Apply(it =>
            {
                it.enabled = false;
                it.ColorRGB = UnityEngine.Color.white;
            });
            Provider.Sepia.Apply(it =>
            {
                it.enabled = false;
                it._Fade = 1;
            });
            Provider.Dream.Apply(it =>
            {
                it.enabled = false;
                it.Distortion = 1;
            });
            Provider.Fisheye.Apply(it =>
            {
                it.enabled = false;
                it.Distortion = 0.5f;
            });
            Provider.Shockwave.Apply(it =>
            {
                it.enabled = false;
                it.TimeX = 1.0f;
                it.Speed = 1;
            });
            Provider.Focus.Apply(it =>
            {
                it.enabled = false;
                it.Size = 1;
                it.Color = UnityEngine.Color.white;
                it.Speed = 5;
                it.Intensity = 0.25f;
            });
            Provider.Glitch.Apply(it =>
            {
                it.enabled = false;
                it.Glitch = 1f;
            });
            Provider.Artifact.Apply(it =>
            {
                it.enabled = false;
                it.Fade = 1;
                it.Colorisation = 1;
                it.Parasite = 1;
                it.Noise = 1;
            });
            Provider.Arcade.Apply(it =>
            {
                it.enabled = false;
                it.Interferance_Size = 1;
                it.Interferance_Speed = 0.5f;
                it.Contrast = 1;
                it.Fade = 1;
            });
            Provider.Chromatical.Apply(it =>
            {
                it.enabled = false;
                it.Fade = 1;
                it.Intensity = 1;
                it.Speed = 1;
            });
            Provider.Tape.Apply(it =>
            {
                it.enabled = false;
            });
            Provider.SleekRender.Apply(it =>
            {
                it.enabled = false;
                it.settings.bloomEnabled = false;
                it.settings.bloomIntensity = 0;
            });
        }

        public void Dispose()
        {
            ComponentRenderers.Values.ForEach(renderers =>
            {
                renderers.ForEach(it => it.Dispose());
            });
            ComponentRenderers.Clear();
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.Storyboard);
            Clear();
        }

        public async UniTask Initialize()
        {
            // Clear
            Clear();

            var timer = new BenchmarkTimer("StoryboardRenderer initialization");
            bool Predicate<TO>(TO obj) where TO : Object => !obj.IsManuallySpawned();
            await SpawnObjects<Text, TextState, TextRenderer>(Storyboard.Texts.Values.ToList(), text => new TextRenderer(this, text), Predicate);
            timer.Time("Text");
            await SpawnObjects<Sprite, SpriteState, SpriteRenderer>(Storyboard.Sprites.Values.ToList(), sprite => new SpriteRenderer(this, sprite), Predicate);
            timer.Time("Sprite");
            await SpawnObjects<Line, LineState, LineRenderer>(Storyboard.Lines.Values.ToList(), line => new LineRenderer(this, line), Predicate);
            timer.Time("Line");
            await SpawnObjects<Video, VideoState, VideoRenderer>(Storyboard.Videos.Values.ToList(), line => new VideoRenderer(this, line), Predicate);
            timer.Time("Video");
            await SpawnObjects<Controller, ControllerState, ControllerRenderer>(Storyboard.Controllers.Values.ToList(), controller => new ControllerRenderer(this, controller), Predicate);
            timer.Time("Controller");
            await SpawnObjects<NoteController, NoteControllerState, NoteControllerRenderer>(Storyboard.NoteControllers.Values.ToList(), noteController => new NoteControllerRenderer(this, noteController), Predicate);
            timer.Time("NoteController");
            timer.Time();

            // Clear on abort/retry/complete
            Game.onGameDisposed.AddListener(_ =>
            {
                Dispose();
            });
            Game.onGamePaused.AddListener(_ =>
            {
                // TODO: Pause SB
            });
            Game.onGameWillUnpause.AddListener(_ =>
            {
                // TODO: Unpause SB
            });
        }

        private async UniTask<List<TR>> SpawnObjects<TO, TS, TR>(List<TO> objects, Func<TO, TR> rendererCreator, Predicate<TO> predicate = default, Func<TO, TO> transformer = default, bool spawnOne = false) 
            where TS : ObjectState
            where TO : Object<TS>
            where TR : StoryboardComponentRenderer<TO, TS>
        {
            if (predicate == default) predicate = _ => true;
            if (transformer == default) transformer = _ => _;
            var renderers = new List<TR>();
            var tasks = new List<UniTask>();
            foreach (var obj in objects)
            {
                if (!predicate(obj)) continue;
                var transformedObj = transformer(obj);
                
                var renderer = rendererCreator(transformedObj);
                if (!ComponentRenderers.ContainsKey(transformedObj.Id))
                {
                    ComponentRenderers[transformedObj.Id] = new List<StoryboardComponentRenderer>();
                }
                ComponentRenderers[transformedObj.Id].Add(renderer);
                // Debug.Log($"StoryboardRenderer: Spawned {typeof(TO).Name} with ID {obj.Id}");
                tasks.Add(renderer.Initialize());
            }

            await UniTask.WhenAll(tasks);
            return renderers;
        }

        public void OnGameUpdate(Game _)
        {
            var time = Time;
            if (time < 0 || Game.State.IsCompleted) return;
            
            var outerRemovals = new List<string>();
            foreach (var id in ComponentRenderers.Keys)
            {
                var renderers = ComponentRenderers[id];
                var innerRemovals = new List<StoryboardComponentRenderer>();
                foreach (var renderer in renderers)
                {
                    renderer.Component.FindStates(time, out var fromState, out var toState);

                    if (fromState == null) continue;

                    // Destroy?
                    if (fromState.Destroy)
                    {
                        if (Game is PlayerGame)
                        {
                            renderer.Clear();
                        }
                        else
                        {
                            renderer.Dispose();
                            innerRemovals.Add(renderer);
                        }
                        continue;
                    }

                    renderer.Update(fromState, toState);
                }

                innerRemovals.ForEach(it =>
                {
                    renderers.Remove(it);
                    if (renderers.Count == 0) outerRemovals.Add(id);
                });
            }
            
            outerRemovals.ForEach(it => ComponentRenderers.Remove(it));
        }

        public void OnTrigger(Trigger trigger)
        {
            // Spawn objects
            if (trigger.Spawn != null)
            {
                foreach (var id in trigger.Spawn)
                {
                    SpawnObjectById(id);
                }
            }

            // Destroy objects
            if (trigger.Destroy != null)
            {
                foreach (var id in trigger.Destroy)
                {
                    DestroyObjectsById(id);
                }
            }
        }

        public async void SpawnObjectById(string id)
        {
            bool Predicate<TO>(TO obj) where TO : Object => obj.Id == id;
            TO Transformer<TO, TS>(TO obj) where TO : Object<TS> where TS : ObjectState
            {
                var res = obj.JsonDeepCopy();
                RecalculateTime<TO, TS>(res);
                return res;
            }
            if (Storyboard.Texts.ContainsKey(id)) await SpawnObjects<Text, TextState, TextRenderer>(new List<Text> {Storyboard.Texts[id]}, text => new TextRenderer(this, text), Predicate, Transformer<Text, TextState>);
            if (Storyboard.Sprites.ContainsKey(id)) await SpawnObjects<Sprite, SpriteState, SpriteRenderer>(new List<Sprite> {Storyboard.Sprites[id]}, sprite => new SpriteRenderer(this, sprite), Predicate, Transformer<Sprite, SpriteState>);
            if (Storyboard.Lines.ContainsKey(id)) await SpawnObjects<Line, LineState, LineRenderer>(new List<Line> {Storyboard.Lines[id]}, line => new LineRenderer(this, line), Predicate, Transformer<Line, LineState>);
            if (Storyboard.Videos.ContainsKey(id)) await SpawnObjects<Video, VideoState, VideoRenderer>(new List<Video> {Storyboard.Videos[id]}, line => new VideoRenderer(this, line), Predicate, Transformer<Video, VideoState>);
            if (Storyboard.Controllers.ContainsKey(id)) await SpawnObjects<Controller, ControllerState, ControllerRenderer>(new List<Controller> {Storyboard.Controllers[id]}, controller => new ControllerRenderer(this, controller), Predicate, Transformer<Controller, ControllerState>);
            if (Storyboard.NoteControllers.ContainsKey(id)) await SpawnObjects<NoteController, NoteControllerState, NoteControllerRenderer>(new List<NoteController> {Storyboard.NoteControllers[id]}, noteController => new NoteControllerRenderer(this, noteController), Predicate, Transformer<NoteController, NoteControllerState>);
        }

        public void DestroyObjectsById(string id)
        {
            if (!ComponentRenderers.ContainsKey(id)) return;
            ComponentRenderers[id].ForEach(it =>
            {
                if (Game is PlayerGame) it.Clear();
                else it.Dispose();
            });
            ComponentRenderers.Remove(id);
        }

        public void RecalculateTime<TO, TS>(TO obj) where TO : Object<TS> where TS : ObjectState
        {
            var baseTime = Time;
            var states = obj.States;

            if (states[0].Time.IsSet())
            {
                baseTime = states[0].Time;
            }
            else
            {
                states[0].Time = baseTime;
            }

            var lastTime = baseTime;
            foreach (var state in states)
            {
                if (state.RelativeTime.IsSet())
                {
                    state.Time = baseTime + state.RelativeTime;
                }

                if (state.AddTime.IsSet())
                {
                    state.Time = lastTime + state.AddTime;
                }

                lastTime = state.Time;
            }
        }

    }
}