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
            if (MainTranslucentImage.Static)
            {
                MainTranslucentImage.Instance.uiCamera.gameObject.SetActive(true);
                MainTranslucentImage.ParallaxElement.gameObject.SetActive(true);
            }
        });
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