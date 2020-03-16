public class ParallaxHolder : SingletonMonoBehavior<ParallaxHolder>
{

    public ParallaxElement Target { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Target = GetComponentInChildren<ParallaxElement>();
    }

    public void Load(ParallaxElement parallax)
    {
        Unload();
        Target = Instantiate(parallax, transform);
    }

    public void Unload()
    {
        if (Target != null)
        {
            Target.Dispose();
            Destroy(Target.gameObject);
        }
    }
}