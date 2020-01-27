using System.Linq.Expressions;
using UniRx.Async;
using UnityEngine;

public class SettingsScreen : Screen, ScreenChangeListener
{
    public const string Id = "Settings";

    public override string GetId() => Id;

    public UpperOverlay upperOverlay;
    public RectTransform generalTab;
    public RectTransform gameplayTab;
    public RectTransform visualTab;
    public RectTransform advancedTab;
    public ContentTabs contentTabs;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        Context.ScreenManager.AddHandler(this);
        
        contentTabs.onTabSelect.AddListener((index, _) =>
        {
            switch (index)
            {
                case 0:
                    upperOverlay.contentRect = generalTab;
                    break;
                case 1:
                    upperOverlay.contentRect = gameplayTab;
                    break;
                case 2:
                    upperOverlay.contentRect = visualTab;
                    break;
                case 3:
                    upperOverlay.contentRect = advancedTab;
                    break;
            }
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        
        SettingsFactory.InstantiateGeneralSettings(generalTab);
        SettingsFactory.InstantiateGameplaySettings(gameplayTab);
        SettingsFactory.InstantiateVisualSettings(visualTab);
        SettingsFactory.InstantiateAdvancedSettings(advancedTab);

        async void Fix(Transform transform)
        {
            LayoutStaticizer.Activate(transform);
            LayoutFixer.Fix(transform);
            await UniTask.DelayFrame(5);
            LayoutStaticizer.Staticize(transform);
        }
        Fix(generalTab.parent);
        Fix(gameplayTab.parent);
        Fix(visualTab.parent);
        Fix(advancedTab.parent);
    }

    public void OnScreenChangeStarted(Screen from, Screen to) => Expression.Empty();

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this)
        {
            foreach (Transform child in generalTab) Destroy(child.gameObject);
            foreach (Transform child in gameplayTab) Destroy(child.gameObject);
            foreach (Transform child in visualTab) Destroy(child.gameObject);
            foreach (Transform child in advancedTab) Destroy(child.gameObject);
        }
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        Context.ScreenManager.RemoveHandler(this);
    }
}