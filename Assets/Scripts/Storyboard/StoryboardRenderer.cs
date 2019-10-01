using System.Collections.Generic;
using System.Linq;
using Cytoid.Storyboard.Controllers;
using Cytoid.Storyboard.Sprites;
using Cytoid.Storyboard.Texts;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public Dictionary<Text, List<UnityEngine.UI.Text>> UiTexts { get; } =
            new Dictionary<Text, List<UnityEngine.UI.Text>>();

        public Dictionary<Sprite, List<UnityEngine.UI.Image>> UiSprites { get; } =
            new Dictionary<Sprite, List<UnityEngine.UI.Image>>();

        public TextEaser TextEaser { get; private set; }
        public SpriteEaser SpriteEaser { get; private set; }
        public List<StoryboardRendererEaser<ControllerState>> ControllerEasers { get; private set; }

        public StoryboardRenderer(Storyboard storyboard)
        {
            Storyboard = storyboard;
        }

        public void Clear()
        {
            // Clear texts
            UiTexts.Keys.ForEach(it => DestroySceneObjects(it.Id));
            UiTexts.Clear();

            // Clear sprites
            UiSprites.Keys.ForEach(it => DestroySceneObjects(it.Id));
            UiSprites.Clear();

            // Clear sprite cache
            Context.SpriteCache.ClearTagged("Storyboard");

            // Initialize easers
            TextEaser = new TextEaser();
            SpriteEaser = new SpriteEaser();
            ControllerEasers = new List<StoryboardRendererEaser<ControllerState>>
            {
                new StoryboardOpacityEaser(),
                new UiOpacityEaser(),
                new ScannerOpacityEaser(),
                new BackgroundDimEaser(),
                new NoteOpacityEaser(),
                new ScannerColorEaser(),
                new ScannerSmoothingEaser(),
                new ScannerPositionEaser(),
                new NoteRingColorEaser(),
                new NoteFillColorEaser(),

                new RadialBlurEaser(),
                new ColorAdjustmentEaser(),
                new GrayScaleEaser(),
                new NoiseEaser(),
                new ColorFilterEaser(),
                new SepiaEaser(),
                new DreamEaser(),
                new FisheyeEaser(),
                new ShockwaveEaser(),
                new FocusEaser(),
                new GlitchEaser(),
                new ArtifactEaser(),
                new ArcadeEaser(),
                new ChromaticalEaser(),
                new TapeEaser(),

                new CameraEaser()
            };

            // Reset camera
            var camera = Provider.Camera;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.transform.eulerAngles = Vector3.zero;
            camera.orthographic = true;
            camera.fieldOfView = 53.2f;
        }

        public async UniTask Initialize()
        {
            // Reset
            Clear();

            // Create initially spawned texts
            foreach (var text in Storyboard.Texts)
            {
                if (!text.IsManuallySpawned())
                {
                    SpawnText(text);
                }
            }

            // Create initially spawned sprites
            var spawnSpriteTasks = new List<UniTask>();
            foreach (var sprite in Storyboard.Sprites)
            {
                if (!sprite.IsManuallySpawned())
                {
                    Debug.Log("Spawned " + sprite.Id);
                    spawnSpriteTasks.Add(SpawnSprite(sprite));
                }
            }

            await UniTask.WhenAll(spawnSpriteTasks);
        }

        public void OnGameUpdate(Game _)
        {
            if (Time < 0) return;
            if (Game.State.IsCompleted)
            {
                Clear();
                return;
            }

            UpdateTexts();
            UpdateSprites();
            UpdateControllers();
        }

        protected virtual void UpdateTexts()
        {
            foreach (var (text, list) in UiTexts.Select(it => (it.Key, it.Value)))
            {
                var removals = new List<UnityEngine.UI.Text>();
                list.ForEach(ui =>
                {
                    FindStates(text.States, out var fromState, out var toState);

                    if (fromState == null) return;

                    // Destroy?
                    if (fromState.Destroy)
                    {
                        Object.Destroy(ui.gameObject);
                        removals.Add(ui);
                        return;
                    }

                    TextEaser.Renderer = this;
                    TextEaser.From = fromState;
                    TextEaser.To = toState;
                    TextEaser.Ease = fromState.Easing;
                    TextEaser.Ui = ui;
                    TextEaser.OnUpdate();
                });
                removals.ForEach(it => list.Remove(it));
            }
        }

        protected virtual void UpdateSprites()
        {
            foreach (var (sprite, list) in UiSprites.Select(it => (it.Key, it.Value)))
            {
                var removals = new List<UnityEngine.UI.Image>();
                list.ForEach(ui =>
                {
                    FindStates(sprite.States, out var fromState, out var toState);

                    if (fromState == null) return;

                    // Destroy?
                    if (fromState.Destroy)
                    {
                        Object.Destroy(ui.gameObject);
                        removals.Add(ui);
                        return;
                    }

                    SpriteEaser.Renderer = this;
                    SpriteEaser.From = fromState;
                    SpriteEaser.To = toState;
                    SpriteEaser.Ease = fromState.Easing;
                    SpriteEaser.Ui = ui;
                    SpriteEaser.OnUpdate();
                });
                removals.ForEach(it => list.Remove(it));
            }
        }

        protected virtual void UpdateControllers()
        {
            foreach (var controller in Storyboard.Controllers)
            {
                FindStates(controller.States, out var fromState, out var toState);
                if (fromState != null)
                {
                    ControllerEasers.ForEach(it =>
                    {
                        it.Renderer = this;
                        it.From = fromState;
                        it.To = toState;
                        it.Ease = fromState.Easing;
                        it.OnUpdate();
                    });
                }
            }
        }

        public void OnTrigger(Trigger trigger)
        {
            // Spawn objects
            if (trigger.Spawn != null)
            {
                foreach (var id in trigger.Spawn)
                {
                    SpawnSceneObject(id);
                }
            }

            // Destroy objects
            if (trigger.Destroy != null)
            {
                foreach (var id in trigger.Destroy)
                {
                    DestroySceneObjects(id);
                }
            }
        }

        public async void SpawnSceneObject(string id)
        {
            foreach (var child in Storyboard.Texts)
            {
                if (child.Id != id) continue;
                var text = child.JsonDeepCopy();
                RecalculateTime(text);
                SpawnText(text);
                break;
            }

            foreach (var child in Storyboard.Sprites)
            {
                if (child.Id != id) continue;
                var sprite = child.JsonDeepCopy();
                RecalculateTime(sprite);
                await SpawnSprite(sprite);
                break;
            }
        }

        public void DestroySceneObjects(string id)
        {
            foreach (var (text, list) in UiTexts.Select(it => (it.Key, it.Value)))
            {
                if (text.Id == id)
                {
                    list.ForEach(it => Object.Destroy(it.gameObject));
                    list.Clear();
                }
            }

            foreach (var (sprite, list) in UiSprites.Select(it => (it.Key, it.Value)))
            {
                if (sprite.Id == id)
                {
                    list.ForEach(it => Object.Destroy(it.gameObject));
                    list.Clear();
                }
            }
        }

        public void SpawnText(Text text)
        {
            var ui = Object.Instantiate(Provider.TextPrefab, Provider.Canvas.transform);
            if (!UiTexts.ContainsKey(text)) UiTexts[text] = new List<UnityEngine.UI.Text>();
            UiTexts[text].Add(ui);
            
            ui.fontSize = 20;
            ui.alignment = TextAnchor.MiddleCenter;
            ui.color = UnityEngine.Color.white;
            ui.GetComponent<CanvasGroup>().alpha = 0;
        }

        public async UniTask SpawnSprite(Sprite sprite)
        {
            var ui = Object.Instantiate(Provider.SpritePrefab, Provider.Canvas.transform);
            if (!UiSprites.ContainsKey(sprite)) UiSprites[sprite] = new List<UnityEngine.UI.Image>();
            UiSprites[sprite].Add(ui);
            
            ui.color = UnityEngine.Color.white;
            ui.preserveAspect = true;
            ui.GetComponent<CanvasGroup>().alpha = 0;

            var path = "file://" + Game.Level.Path + sprite.States[0].Path;
            ui.sprite = await Context.SpriteCache.CacheSprite(path, "Storyboard");
        }

        public void RecalculateTime<T>(Object<T> obj) where T : ObjectState
        {
            var baseTime = Time;

            if (obj.States[0].Time.IsSet())
            {
                baseTime = obj.States[0].Time;
            }
            else
            {
                obj.States[0].Time = baseTime;
            }

            var lastTime = baseTime;
            foreach (var state in obj.States)
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

        private void FindStates<T>(List<T> states, out T currentState, out T nextState) where T : ObjectState
        {
            if (states.Count == 0)
            {
                currentState = null;
                nextState = null;
                return;
            }

            for (var i = 0; i < states.Count; i++)
                if (states[i].Time > Time) // Next state
                {
                    // Current state is the previous state
                    currentState = i > 0 ? states[i - 1] : null;
                    nextState = states[i];
                    return;
                }

            currentState = states.Last();
            nextState = currentState;
        }
    }
}