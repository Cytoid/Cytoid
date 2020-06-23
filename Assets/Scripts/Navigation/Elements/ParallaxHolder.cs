using System;
using LeTai.Asset.TranslucentImage;
using UniRx.Async;

public class ParallaxHolder : SingletonMonoBehavior<ParallaxHolder>
{
    public static bool WillDelaySet = false;

    public ParallaxElement Target { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Target = GetComponentInChildren<ParallaxElement>();
        Context.CharacterManager.OnActiveCharacterSet.AddListener(async it =>
        {
            if (WillDelaySet) await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
            Load(it.parallaxPrefab);
        });
    }

    private void Update()
    {
        if (Target != null)
        {
            var shouldActive = NavigationBackdrop.Instance.ShouldParallaxActive;
            if (shouldActive && !Target.gameObject.activeInHierarchy)
            {
                Target.gameObject.SetActive(true);
            } 
            else if (!shouldActive && Target.gameObject.activeInHierarchy)
            {
                Target.gameObject.SetActive(false);
            }
        }
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