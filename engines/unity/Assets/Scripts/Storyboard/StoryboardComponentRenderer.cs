using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cytoid.Storyboard
{
    public abstract class StoryboardComponentRenderer
    {
        public Object Component { get; protected set; }
        
        public List<StoryboardComponentRenderer> Children = new List<StoryboardComponentRenderer>();

        public StoryboardComponentRenderer Parent;

        /// <summary>Storyboard object type key used in <see cref="StoryboardRenderer.TypedComponentRenderers"/>.</summary>
        public abstract Type ObjectType { get; }
        
        public abstract Transform Transform { get; }
        
        public abstract Transform CanvasTransform { get; }
        
        public abstract Transform WorldTransform { get; }

        public abstract bool IsOnCanvas { get; }

        public bool IsDisposed { get; private set; }

        protected int InitializeVersion { get; private set; }

        public abstract UniTask Initialize();

        public abstract void Clear();

        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            InitializeVersion++;
            Children.Clear();
            Parent = null;
        }

        public abstract void Update(ObjectState fromState, ObjectState toState);

        protected int BeginInitialize() => ++InitializeVersion;

        protected bool IsInitializeStale(int version) => IsDisposed || version != InitializeVersion;

        public bool IsTransformActive
        {
            get => isTransformActive;
            set
            {
                if (isTransformActive != value)
                {
                    isTransformActive = value;
                    if (Transform != null)
                        Transform.gameObject.SetActive(value);
                }
            }
        }

        private bool isTransformActive;
        
    }
    
    public abstract class StoryboardComponentRenderer<TO, TS> : StoryboardComponentRenderer where TS : ObjectState where TO : Object<TS>
    {

        public StoryboardRenderer MainRenderer { get; private set; }
        
        public StoryboardRendererEaser<TS> Easer { get; private set; }

        public new TO Component { get; private set; }

        public override Type ObjectType => typeof(TO);

        public StoryboardRendererProvider Provider => StoryboardRendererProvider.Instance;

        public StoryboardComponentRenderer(StoryboardRenderer mainRenderer, TO component)
        {
            MainRenderer = mainRenderer;
            Component = component;
            base.Component = component;
        }

        public abstract StoryboardRendererEaser<TS> CreateEaser();

        public override void Dispose()
        {
            if (IsDisposed) return;
            MainRenderer = null;
            Component = null;
            if (equivalentTransform != null)
            {
                UnityEngine.Object.Destroy(equivalentTransform);
            }
            equivalentTransform = null;
            base.Dispose();
        }

        public virtual void Update(TS fromState, TS toState)
        {
            if (Easer == null) Easer = CreateEaser();
            Easer.From = fromState;
            Easer.To = toState;
            Easer.Ease = fromState.Easing ?? EasingFunction.Ease.Linear;
            Easer.Time = MainRenderer.Time;
            Easer.OnUpdate();
        }

        public override void Update(ObjectState fromState, ObjectState toState)
        {
            if (IsDisposed || MainRenderer == null) return;
            Update((TS) fromState, (TS) toState);
            UpdateEquivalentTransform();
        }

        /*
         * TODO: Refactor this? Seems unnecessary since StoryboardRenderer.SpawnObjects does the same job
         */
        protected T GetTargetRenderer<T>() where T : StoryboardComponentRenderer
        {
            if (Component.TargetId != null)
            {
                if (!MainRenderer.ComponentRenderers.ContainsKey(Component.TargetId))
                {
                    throw new InvalidOperationException($"Storyboard: target_id \"{Component.TargetId}\" does not exist");
                }

                var typedRenderer = MainRenderer.ComponentRenderers[Component.TargetId] as T;
                if (typedRenderer == null)
                {
                    throw new InvalidOperationException($"Storyboard: target_id \"{Component.TargetId} does not have type {typeof(T).Name}");
                }

                return typedRenderer;
            }

            return null;
        }

        protected virtual Transform DefaultParentTransform => IsOnCanvas ? Provider.CanvasRectTransform : MainRenderer.Game.contentParent.transform;

        public override Transform CanvasTransform
        {
            get
            {
                if (IsOnCanvas) return Transform;
                if (equivalentTransform != null) return equivalentTransform.transform;
                InitializeTransformPlaceholder();
                return equivalentTransform.transform;
            }
        }

        public override Transform WorldTransform
        {
            get
            {
                if (!IsOnCanvas) return Transform;
                if (equivalentTransform != null) return equivalentTransform.transform;
                InitializeTransformPlaceholder();
                return equivalentTransform.transform;
            }
        }

        private GameObject equivalentTransform;

        protected void InitializeTransformPlaceholder()
        {
            if (Transform == null) throw new InvalidOperationException();
            equivalentTransform = new GameObject($"TransformEquivalent_{Component.Id}");
            if (IsOnCanvas)
            {
                // Initialize a world equivalent
                equivalentTransform.transform.parent = MainRenderer.Game.contentParent.transform;
            }
            else
            {
                // Initialize a canvas equivalent
                equivalentTransform.transform.parent = Provider.CanvasRectTransform;
                var rectTransform = equivalentTransform.AddComponent<RectTransform>();
                rectTransform.anchorMin = rectTransform.anchorMax = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
            equivalentTransform.transform.localPosition = Vector3.zero;
            equivalentTransform.transform.localScale = Vector3.one;
            
            UpdateEquivalentTransform();
        }

        protected virtual void UpdateEquivalentTransform()
        {
            // Don't update if not accessed
            if (equivalentTransform != null)
            {
                if (IsOnCanvas)
                {
                    var rectTransform = Transform as RectTransform;
                    if (rectTransform == null) throw new InvalidOperationException();
                    // Canvas -> World
                    var anchoredPos = rectTransform.anchoredPosition;
                    var screenPos = new Vector2(
                        anchoredPos.x * MainRenderer.Constants.CanvasToWorldXMultiplier,
                        anchoredPos.y * MainRenderer.Constants.CanvasToWorldYMultiplier
                    );
                    var worldPos = MainRenderer.Camera.ScreenToWorldPoint(screenPos);
                    equivalentTransform.transform.position = worldPos;
                }
                else
                {
                    var rectTransform = equivalentTransform.transform as RectTransform;
                    if (rectTransform == null) throw new InvalidOperationException();
                    // World -> Canvas
                    var pos = Transform.position;
                    var screenPos = MainRenderer.Camera.WorldToScreenPoint(pos);
                    
                    rectTransform.anchoredPosition = new Vector2(
                        screenPos.x * MainRenderer.Constants.WorldToCanvasXMultiplier,
                        screenPos.y * MainRenderer.Constants.WorldToCanvasYMultiplier
                    );
                }
            }
        }
        
        protected virtual Transform GetParentTransform()
        {
            if (Component.ParentId != null)
            {
                var parentRenderer = MainRenderer.ComponentRenderers[Component.ParentId];
                return IsOnCanvas ? parentRenderer.CanvasTransform : parentRenderer.WorldTransform;
            }
            return DefaultParentTransform;
        }

    }
    
}
