using System;
using System.Collections.Generic;
using System.Linq;
using Cytoid.Storyboard.Controllers;
using Cytoid.Storyboard.PostProcess;
using Cytoid.Storyboard.Sprites;
using Cytoid.Storyboard.Texts;
using Cytoid.Storyboard.Videos;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Video;
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
        public Camera Camera { get; private set; }
        public float Time => Game.Time;
        public StoryboardRendererProvider Provider => StoryboardRendererProvider.Instance;
        
        public readonly Dictionary<string, StoryboardComponentRenderer> ComponentRenderers = 
            new Dictionary<string, StoryboardComponentRenderer>(); // Object ID to renderer instance

        public readonly Dictionary<Type, List<StoryboardComponentRenderer>> TypedComponentRenderers =
            new Dictionary<Type, List<StoryboardComponentRenderer>>();
        
        public readonly Dictionary<string, int> SpritePathRefCount = new Dictionary<string, int>();
        
        public StoryboardConstants Constants { get; } = new StoryboardConstants();
        
        public class StoryboardConstants
        {
            public float CanvasToWorldXMultiplier;
            public float CanvasToWorldYMultiplier;
            public float WorldToCanvasXMultiplier;
            public float WorldToCanvasYMultiplier;
        }

        public StoryboardRenderer(Storyboard storyboard)
        {
            Storyboard = storyboard;
        }

        public void Clear()
        {
            ComponentRenderers.Values.ForEach(it => it.Clear());

            ResetCamera();
            ResetCameraFilters();
            
            var canvas = Provider.CanvasRect;
            Constants.CanvasToWorldXMultiplier = 1.0f / canvas.width * Camera.pixelWidth;
            Constants.CanvasToWorldYMultiplier = 1.0f / canvas.height * Camera.pixelHeight;
            Constants.WorldToCanvasXMultiplier = 1.0f / Camera.pixelWidth * canvas.width;
            Constants.WorldToCanvasYMultiplier = 1.0f / Camera.pixelHeight * canvas.height;
        }

        private void ResetCamera()
        {
            Camera = Provider.Camera;
            var cameraTransform = Camera.transform;
            cameraTransform.position = new Vector3(0, 0, -10);
            cameraTransform.eulerAngles = Vector3.zero;
            Camera.orthographic = true;
            Camera.fieldOfView = 53.2f;
        }

        private void ResetCameraFilters()
        {
            if (StoryboardEffects.Current != null)
                StoryboardEffects.Current.ResetToDefaults();
            else
                Provider.Effects.ResetToDefaults();
        }

        public void Dispose()
        {
            ComponentRenderers.Values.ForEach(it => it.Dispose());
            ComponentRenderers.Clear();
            TypedComponentRenderers.Clear();
            SpritePathRefCount.Clear();
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.Storyboard);
            Clear();
        }

        public async UniTask Initialize()
        {
            // Clear
            Clear();

            foreach (var type in new[]
                {typeof(Text), typeof(Sprite), typeof(Line), typeof(Video), typeof(Controller), typeof(NoteController)})
            {
                TypedComponentRenderers[type] = new List<StoryboardComponentRenderer>();
            }
            
            var timer = new BenchmarkTimer("StoryboardRenderer initialization");
            bool Predicate<TO>(TO obj) where TO : Object => !obj.IsManuallySpawned(); await SpawnObjects<NoteController, NoteControllerState, NoteControllerRenderer>(Storyboard.NoteControllers.Values.ToList(), noteController => new NoteControllerRenderer(this, noteController), Predicate);
            timer.Time("NoteController"); // Spawn note placeholder transforms
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
            timer.Time();

            // Clear on abort/retry/complete
            Game.onGameDisposed.AddListener(_ =>
            {
                Dispose();
            });
            Game.onGamePaused.AddListener(_ => SyncAllVideoPlayback());
            Game.onGameUnpaused.AddListener(_ => SyncAllVideoPlayback());
        }

        private void SyncAllVideoPlayback()
        {
            if (!TypedComponentRenderers.TryGetValue(typeof(Video), out var renderers)) return;

            var syncedPlayers = new HashSet<VideoPlayer>();
            foreach (var renderer in renderers)
            {
                if (renderer is not VideoRenderer videoRenderer) continue;
                var player = videoRenderer.VideoPlayer;
                if (player == null || !syncedPlayers.Add(player)) continue;
                videoRenderer.SyncPlaybackWithGameState();
            }
        }

        private async UniTask<List<TR>> SpawnObjects<TO, TS, TR>(List<TO> objects, Func<TO, TR> rendererCreator, Predicate<TO> predicate = default, Func<TO, TO> transformer = default) 
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
                if (ComponentRenderers.ContainsKey(transformedObj.Id))
                {
                    Debug.LogWarning($"Storyboard: Object {transformedObj.Id} is already spawned");
                    continue;
                }
                ComponentRenderers[transformedObj.Id] = renderer;
                TypedComponentRenderers[typeof(TO)].Add(renderer);
                // Debug.Log($"StoryboardRenderer: Spawned {typeof(TO).Name} with ID {obj.Id}");
                
                // Resolve parent
                StoryboardComponentRenderer parent = null;
                if (transformedObj.ParentId != null)
                {
                    if (!ComponentRenderers.ContainsKey(transformedObj.ParentId))
                    {
                        throw new InvalidOperationException($"Storyboard: parent_id \"{transformedObj.ParentId}\" does not exist");
                    }
                    parent = ComponentRenderers[transformedObj.ParentId];
                }
                else if (transformedObj.TargetId != null)
                {
                    if (!ComponentRenderers.ContainsKey(transformedObj.TargetId))
                    {
                        throw new InvalidOperationException($"Storyboard: target_id \"{transformedObj.TargetId}\" does not exist");
                    }
                    parent = ComponentRenderers[transformedObj.TargetId] as TR ?? throw new InvalidOperationException($"Storyboard: target_id \"{transformedObj.TargetId} does not have type {typeof(TR).Name}");
                }
                if (parent != null)
                {
                    parent.Children.Add(renderer);
                    renderer.Parent = parent;
                }
                
                tasks.Add(renderer.Initialize());
            }

            await UniTask.WhenAll(tasks);
            return renderers;
        }

        public void OnGameUpdate(Game _)
        {
            var time = Time;
            if (time < 0 || Game.State.IsReadyToExit) return;

            var updateOrder = new[]
                {typeof(NoteController), typeof(Text), typeof(Sprite), typeof(Line), typeof(Video), typeof(Controller)};

            // Insertion order is parent/self then descendants; destroy in reverse so
            // children run first (same invariant as DestroyObjectsById).
            var renderersToDestroy = new List<string>();
            var renderersToDestroySet = new HashSet<string>();
            void EnqueueDestroy(string id)
            {
                if (renderersToDestroySet.Add(id))
                    renderersToDestroy.Add(id);
            }

            foreach (var type in updateOrder)
            {
                var renderers = TypedComponentRenderers[type];
                foreach (var renderer in renderers)
                {
                    renderer.Component.FindStates(time, out var fromState, out var toState);

                    if (fromState == null) continue;

                    // Destroy?
                    if (fromState.Destroy != null && fromState.Destroy.Value)
                    {
                        // Destroy the target as well
                        if (renderer.Parent != null && renderer.Component.TargetId != null)
                        {
                            EnqueueDestroy(renderer.Parent.Component.Id);
                            this.ListOf(renderer.Parent).Flatten(it => it.Children).ForEach(it =>
                            {
                                EnqueueDestroy(it.Component.Id);
                            });
                        }
                        else
                        {
                            renderer.Parent?.Children.Remove(renderer);
                            this.ListOf(renderer).Flatten(it => it.Children).ForEach(it =>
                            {
                                EnqueueDestroy(it.Component.Id);
                            });
                        }
                        continue;
                    }

                    renderer.Update(fromState, toState);
                }
            }

            for (var i = renderersToDestroy.Count - 1; i >= 0; i--)
            {
                var id = renderersToDestroy[i];
                if (!ComponentRenderers.TryGetValue(id, out var renderer)) continue;
                DestroyObject(id, renderer);
            }
        }

        public void OnTrigger(Trigger trigger)
        {
            // Spawn objects
            if (trigger.Spawn != null)
            {
                foreach (var id in trigger.Spawn)
                {
                    SpawnObjectByIdLogged(id).Forget();
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

        async UniTaskVoid SpawnObjectByIdLogged(string id)
        {
            try
            {
                await SpawnObjectById(id);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public async UniTask SpawnObjectById(string id)
        {
            // Storyboard object ids are unique across component kinds; else-if matches that contract
            // (legacy multi-if could spawn the same id from multiple dictionaries).
            bool Predicate<TO>(TO obj) where TO : Object => obj.Id == id;
            TO Transformer<TO, TS>(TO obj) where TO : Object<TS> where TS : ObjectState
            {
                var res = obj.JsonDeepCopy();
                RecalculateTime<TO, TS>(res);
                return res;
            }
            if (Storyboard.Texts.ContainsKey(id)) await SpawnObjects<Text, TextState, TextRenderer>(new List<Text> {Storyboard.Texts[id]}, text => new TextRenderer(this, text), Predicate, Transformer<Text, TextState>);
            else if (Storyboard.Sprites.ContainsKey(id)) await SpawnObjects<Sprite, SpriteState, SpriteRenderer>(new List<Sprite> {Storyboard.Sprites[id]}, sprite => new SpriteRenderer(this, sprite), Predicate, Transformer<Sprite, SpriteState>);
            else if (Storyboard.Lines.ContainsKey(id)) await SpawnObjects<Line, LineState, LineRenderer>(new List<Line> {Storyboard.Lines[id]}, line => new LineRenderer(this, line), Predicate, Transformer<Line, LineState>);
            else if (Storyboard.Videos.ContainsKey(id)) await SpawnObjects<Video, VideoState, VideoRenderer>(new List<Video> {Storyboard.Videos[id]}, line => new VideoRenderer(this, line), Predicate, Transformer<Video, VideoState>);
            else if (Storyboard.Controllers.ContainsKey(id)) await SpawnObjects<Controller, ControllerState, ControllerRenderer>(new List<Controller> {Storyboard.Controllers[id]}, controller => new ControllerRenderer(this, controller), Predicate, Transformer<Controller, ControllerState>);
            else if (Storyboard.NoteControllers.ContainsKey(id)) await SpawnObjects<NoteController, NoteControllerState, NoteControllerRenderer>(new List<NoteController> {Storyboard.NoteControllers[id]}, noteController => new NoteControllerRenderer(this, noteController), Predicate, Transformer<NoteController, NoteControllerState>);
        }

        public void DestroyObjectsById(string id)
        {
            if (!ComponentRenderers.TryGetValue(id, out var renderer)) return;

            // Cascade children/borrowers first so owner teardown cannot leave dangling shared refs.
            var children = renderer.Children.ToList();
            foreach (var child in children)
            {
                if (child?.Component?.Id != null)
                    DestroyObjectsById(child.Component.Id);
            }

            if (!ComponentRenderers.TryGetValue(id, out renderer)) return;
            DestroyObject(id, renderer);
        }

        public void DestroyObject(string id, StoryboardComponentRenderer renderer)
        {
            if (renderer == null) return;

            DetachFromParent(renderer);
            renderer.Dispose();

            if (TypedComponentRenderers.TryGetValue(renderer.ObjectType, out var list))
                list.Remove(renderer);
            ComponentRenderers.Remove(id);
        }

        private static void DetachFromParent(StoryboardComponentRenderer renderer)
        {
            if (renderer.Parent == null) return;
            renderer.Parent.Children.Remove(renderer);
            renderer.Parent = null;
        }

        public void RecalculateTime<TO, TS>(TO obj) where TO : Object<TS> where TS : ObjectState
        {
            var baseTime = Time;
            var states = obj.States;

            if (obj.IsManuallySpawned())
            {
                states[0].Time = baseTime;
            }
            else
            {
                baseTime = states[0].Time;
            }

            var lastTime = baseTime;
            foreach (var state in states)
            {
                if (state.RelativeTime != null)
                {
                    state.Time = baseTime + state.RelativeTime.Value;
                }

                if (state.AddTime != null)
                {
                    state.Time = lastTime + state.AddTime.Value;
                }

                lastTime = state.Time;
            }
        }

    }
}