using UniRx.Async;

namespace Cytoid.Storyboard
{
    public abstract class StoryboardComponentRenderer
    {
        public Object Component { get; protected set; }
        
        public abstract UniTask Initialize();

        public abstract void Clear();

        public abstract void Dispose();

        public abstract void Update(ObjectState fromState, ObjectState toState);
    }
    
    public abstract class StoryboardComponentRenderer<TO, TS> : StoryboardComponentRenderer where TS : ObjectState where TO : Object<TS>
    {

        public StoryboardRenderer MainRenderer { get; private set; }
        
        public StoryboardRendererEaser<TS> Easer { get; private set; }

        public new TO Component { get; private set; }

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
            MainRenderer = null;
            Component = null;
        }

        public virtual void Update(TS fromState, TS toState)
        {
            if (Easer == null) Easer = CreateEaser();
            Easer.From = fromState;
            Easer.To = toState;
            Easer.Ease = fromState.Easing;
            Easer.Time = MainRenderer.Time;
            Easer.OnUpdate();
        }

        public override void Update(ObjectState fromState, ObjectState toState)
        {
            Update((TS) fromState, (TS) toState);
        }

    }
}