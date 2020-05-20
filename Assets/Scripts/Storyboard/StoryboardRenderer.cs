using System;
using System.Collections.Generic;
using System.Linq;
using Cytoid.Storyboard.Controllers;
using Cytoid.Storyboard.Lines;
using Cytoid.Storyboard.Notes;
using Cytoid.Storyboard.Sprites;
using Cytoid.Storyboard.Texts;
using Newtonsoft.Json;
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

        public Dictionary<Text, UnityEngine.UI.Text> UiTexts { get; } =
            new Dictionary<Text, UnityEngine.UI.Text>();

        public Dictionary<Sprite, UnityEngine.UI.Image> UiSprites { get; } =
            new Dictionary<Sprite, UnityEngine.UI.Image>();

        public Dictionary<Line, LineRenderer> LineRenderers { get; } =
            new Dictionary<Line, LineRenderer>();

        public Dictionary<string, Text> TextLookup { get; } = new Dictionary<string, Text>();
        public Dictionary<string, Sprite> SpriteLookup { get; } = new Dictionary<string, Sprite>();

        public TextEaser TextEaser { get; private set; }
        public SpriteEaser SpriteEaser { get; private set; }
        public LineEaser LineEaser { get; private set; }
        public NoteControllerEaser NoteControllerEaser { get; private set; }
        public List<StoryboardRendererEaser<ControllerState>> ControllerEasers { get; private set; }

        public StoryboardRenderer(Storyboard storyboard)
        {
            Storyboard = storyboard;
        }

        public void Clear()
        {
            UiTexts.Keys.ForEach(it => DestroyText(it));
            UiSprites.Keys.ForEach(it => DestroySprite(it));            
            LineRenderers.Keys.ForEach(it => DestroyLine(it));            

            // Initialize easers
            TextEaser = new TextEaser();
            SpriteEaser = new SpriteEaser();
            LineEaser = new LineEaser();
            NoteControllerEaser = new NoteControllerEaser();
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
                new GlobalNoteRingColorEaser(),
                new GlobalNoteFillColorEaser(),

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
                new BloomEaser(),

                new CameraEaser()
            };

            // Reset camera
            var camera = Provider.Camera;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.transform.eulerAngles = Vector3.zero;
            camera.orthographic = true;
            camera.fieldOfView = 53.2f;
        }

        public void Dispose()
        {
            var texts = new List<Text>(UiTexts.Keys);
            texts.ForEach(it => DestroyText(it, true));
            UiTexts.Clear();
            var sprites = new List<Sprite>(UiSprites.Keys);
            sprites.ForEach(it => DestroySprite(it, true));
            UiSprites.Clear();
            var lines = new List<Line>(LineRenderers.Keys);
            lines.ForEach(it => DestroyLine(it, true));
            LineRenderers.Clear();
            Clear();
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.Storyboard);
        }

        public async UniTask Initialize()
        {
            // Clear
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
                    spawnSpriteTasks.Add(SpawnSprite(sprite));
                }
            }

            await UniTask.WhenAll(spawnSpriteTasks);
            
            // Create initially spawned lines
            foreach (var line in Storyboard.Lines)
            {
                if (!line.IsManuallySpawned())
                {
                    SpawnLine(line);
                }
            }
            
            // Clear on abort/retry/complete
            Game.onGameDisposed.AddListener(_ =>
            {
                Clear();
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

        public void OnGameUpdate(Game _)
        {
            if (Time < 0 || Game.State.IsCompleted) return;

            UpdateTexts();
            UpdateSprites();
            UpdateLines();
            UpdateControllers();
            UpdateNoteControllers();
        }

        protected virtual void UpdateTexts()
        {
            var removals = new List<Text>();
            foreach (var (text, ui) in UiTexts.Select(it => (it.Key, it.Value)))
            {
                FindStates(text.States, out var fromState, out var toState);

                if (fromState == null) continue;

                // Destroy?
                if (fromState.Destroy)
                {
                    removals.Add(text);
                    continue;
                }

                TextEaser.Renderer = this;
                TextEaser.From = fromState;
                TextEaser.To = toState;
                TextEaser.Ease = fromState.Easing;
                TextEaser.Ui = ui;
                TextEaser.OnUpdate();
            }
            removals.ForEach(it => DestroyText(it));
        }

        protected virtual void UpdateSprites()
        {
            var removals = new List<Sprite>();
            foreach (var (sprite, ui) in UiSprites.Select(it => (it.Key, it.Value)))
            {
                FindStates(sprite.States, out var fromState, out var toState);

                if (fromState == null) continue;

                // Destroy?
                if (fromState.Destroy)
                {
                    removals.Add(sprite);
                    continue;
                }

                SpriteEaser.Renderer = this;
                SpriteEaser.From = fromState;
                SpriteEaser.To = toState;
                SpriteEaser.Ease = fromState.Easing;
                SpriteEaser.Ui = ui;
                SpriteEaser.OnUpdate();
            }
            removals.ForEach(it => DestroySprite(it));
        }

        protected virtual void UpdateLines()
        {
            var removals = new List<Line>();
            foreach (var (line, renderer) in LineRenderers.Select(it => (it.Key, it.Value)))
            {
                FindStates(line.States, out var fromState, out var toState);

                if (fromState == null) continue;

                // Destroy?
                if (fromState.Destroy)
                {
                    removals.Add(line);
                    continue;
                }

                LineEaser.Renderer = this;
                LineEaser.From = fromState;
                LineEaser.To = toState;
                LineEaser.Ease = fromState.Easing;
                LineEaser.Line = renderer;
                LineEaser.OnUpdate();
            }
            removals.ForEach(it => DestroyLine(it));
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
        
        protected virtual void UpdateNoteControllers()
        {
            foreach (var controller in Storyboard.NoteControllers)
            {
                FindStates(controller.States, out var fromState, out var toState);
                if (fromState != null)
                {
                    NoteControllerEaser.Renderer = this;
                    NoteControllerEaser.From = fromState;
                    NoteControllerEaser.To = toState;
                    NoteControllerEaser.Ease = fromState.Easing;
                    NoteControllerEaser.OnUpdate();
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
                    SpawnObject(id);
                }
            }

            // Destroy objects
            if (trigger.Destroy != null)
            {
                foreach (var id in trigger.Destroy)
                {
                    DestroyObjects(id);
                }
            }
        }

        public async void SpawnObject(string id)
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

            foreach (var child in Storyboard.Lines)
            {
                if (child.Id != id) continue;
                var line = child.JsonDeepCopy();
                RecalculateTime(line);
                SpawnLine(line);
                break;
            }
        }

        public void DestroyObjects(string id)
        {
            UiTexts.Keys.Where(it => it.Id == id).ForEach(it => DestroyText(it));
            UiSprites.Keys.Where(it => it.Id == id).ForEach(it => DestroySprite(it));
        }

        public void SpawnText(Text text)
        {
            var ui = Object.Instantiate(Provider.TextPrefab, Provider.Canvas.transform);
            if (UiTexts.ContainsKey(text)) DestroyText(text, true);
            UiTexts[text] = ui;
            
            ui.fontSize = 20;
            ui.alignment = TextAnchor.MiddleCenter;
            ui.color = UnityEngine.Color.white;
            ui.GetComponent<CanvasGroup>().alpha = 0;
        }

        public async UniTask SpawnSprite(Sprite sprite)
        {
            var ui = Object.Instantiate(Provider.SpritePrefab, Provider.Canvas.transform);
            if (UiSprites.ContainsKey(sprite)) DestroySprite(sprite, true);
            UiSprites[sprite] = ui;
            
            ui.color = UnityEngine.Color.white;
            ui.preserveAspect = true;
            ui.GetComponent<CanvasGroup>().alpha = 0;
            
            var spritePath = sprite.States[0].Path;
            if (spritePath == null && sprite.States.Count > 1) spritePath = sprite.States[1].Path;
            if (spritePath == null)
            {
                throw new InvalidOperationException("Sprite does not have a valid path");
            }

            var path = "file://" + Game.Level.Path + spritePath;
            ui.sprite = await Context.AssetMemory.LoadAsset<UnityEngine.Sprite>(path, AssetTag.Storyboard);
        }

        public void SpawnLine(Line line)
        {
            var gameObject = new GameObject("Line_" + line.Id);
            gameObject.transform.parent = Game.contentParent.transform;
            var renderer = gameObject.AddComponent<LineRenderer>();
            if (LineRenderers.ContainsKey(line)) DestroyLine(line, true);
            LineRenderers[line] = renderer;
            
            renderer.positionCount = 0;
            renderer.startColor = renderer.endColor = UnityEngine.Color.white.WithAlpha(0);
            renderer.startWidth = renderer.endWidth = 0.05f;
            renderer.material = Scanner.Instance.lineRenderer.material;
        }

        public void DestroyText(Text text, bool forceDestroy = false)
        {
            if (!UiTexts.ContainsKey(text)) return;
            if (!forceDestroy && Game is PlayerGame)
            {
                var ui = UiTexts[text];
                ui.fontSize = 20;
                ui.alignment = TextAnchor.MiddleCenter;
                ui.color = UnityEngine.Color.white;
                ui.GetComponent<CanvasGroup>().alpha = 0;
            }
            else
            {
                Object.Destroy(UiTexts[text]);
                UiTexts.Remove(text);
            }
        }
        
        public void DestroySprite(Sprite sprite, bool forceDestroy = false)
        {
            if (!UiSprites.ContainsKey(sprite)) return;

            if (!forceDestroy && Game is PlayerGame)
            {
                var ui = UiSprites[sprite];
                ui.color = UnityEngine.Color.white;
                ui.preserveAspect = true;
                ui.GetComponent<CanvasGroup>().alpha = 0;
            }
            else
            {
                Object.Destroy(UiSprites[sprite]);
                UiSprites.Remove(sprite);
            }
        }
        
        public void DestroyLine(Line line, bool forceDestroy = false)
        {
            if (!LineRenderers.ContainsKey(line)) return;

            if (!forceDestroy && Game is PlayerGame)
            {
                var renderer = LineRenderers[line];
                renderer.positionCount = 0;
                renderer.SetPositions(new Vector3[]{});
                renderer.startColor = renderer.endColor = UnityEngine.Color.white.WithAlpha(0);
                renderer.startWidth = renderer.endWidth = 0.05f;
            }
            else
            {
                Object.Destroy(LineRenderers[line]);
                LineRenderers.Remove(line);
            }
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

            currentState = nextState = states.Last();
        }
    }
}