using System.Collections.Generic;
using System.Linq.Expressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionTabs : MonoBehaviour, ScreenChangeListener
{
    
    [HideInInspector] public TabChangeEvent OnTabChanged = new TabChangeEvent();
    
    private float animationDuration = 0.4f;
    
    public List<GameObject> icons;
    public List<TransitionElement> tabs;
    public TransitionElement tabBackground;
    public RectangularDetectionArea closeDetectionArea;

    public string closeGameObjectName = "Close";
    public string iconPath = "Graphic/Icon";
    public string tabIndicatorPath = "Graphic/TabIndicator";

    [GetComponent] public HorizontalLayoutGroup horizontalLayoutGroup;
    private RectTransform profileRectTransform;
    
    private Action closeAction;
    private List<Action> actions = new List<Action>();
    private int currentActionIndex;
    private float lastProfileRectWidth;

    private void Awake()
    {
        // Make sure game objects are active
        icons.ForEach(it => it.gameObject.SetActive(true));
        tabs.ForEach(it =>
        {
            it.gameObject.SetActive(true);
            it.canvasGroup.blocksRaycasts = false;
        });
        tabBackground.gameObject.SetActive(true);
        tabBackground.canvasGroup.blocksRaycasts = false;
        
        // Get profile rect transform
        if (ProfileWidget.Instance != null)
        {
            profileRectTransform = ProfileWidget.Instance.GetComponent<RectTransform>();
        }

        // Setup close button
        var closeIcon = gameObject.transform.Find(closeGameObjectName);
        closeAction = closeIcon.gameObject.AddComponent<Action>();
        closeAction.owner = this;
        closeAction.index = -1;
        closeAction.icon = closeIcon.Find(iconPath);
        DOFade(closeAction.icon, 0, 0);

        // Setup actions and tabs
        for (var index = 0; index < icons.Count; index++)
        {
            var actionGameObject = icons[index];
            var icon = actionGameObject.transform.Find(iconPath);
            var tabIndicator = actionGameObject.transform.Find(tabIndicatorPath);
            if (icon == null || tabIndicator == null) continue;

            var action = actionGameObject.AddComponent<Action>();
            action.owner = this;
            action.index = index;
            action.icon = icon;
            action.tabIndicator = tabIndicator.GetComponent<TransitionElement>();
            action.tabIndicator.hiddenOnStart = true;
            action.tabIndicator.enterOnScreenBecomeActive = false;

            actions.Add(action);
            
            tabs[index].hiddenOnStart = true;
            tabs[index].enterOnScreenBecomeActive = false;
        }

        tabBackground.hiddenOnStart = true;
        tabBackground.enterOnScreenBecomeActive = false;
        
        // Set up close detection area
        closeDetectionArea.onClick = Close;
        closeDetectionArea.DetectionEnabled = false;
    }

    private void Start()
    {
        Context.ScreenManager.AddHandler(this);
    }

    public void Close()
    {
        OnAction(closeAction);
    }

    private void Update()
    {
        var profileRect = profileRectTransform.rect;
        if (profileRectTransform != null && !Mathf.Approximately(lastProfileRectWidth, profileRect.width))
        {
            lastProfileRectWidth = profileRect.width;
            // Set padding based on profile
            horizontalLayoutGroup.padding.right = (int) profileRect.width - 24;
            LayoutFixer.Fix(horizontalLayoutGroup.transform);
        }
    }

    public void OnAction(Action action)
    {
        var prev = currentActionIndex == -1 ? closeAction : actions[currentActionIndex];
        var enterTransition = action.index < currentActionIndex ? Transition.Right : Transition.Left;
        var leaveTransition = action.index < currentActionIndex ? Transition.Left : Transition.Right;
        if (action.index == -1)
        {
            // Close
            tabBackground.Leave();
            actions.ForEach(it =>
            {
                DOFade(it.icon, 1f, animationDuration);
                it.tabIndicator.leaveTo = leaveTransition;
                it.tabIndicator.Leave();
            });
            tabs.ForEach(it =>
            {
                it.leaveTo = Transition.Right; // TODO: Customize?
                it.Leave();
            });
                
            DOFade(closeAction.icon, 0, animationDuration);
            
            closeDetectionArea.DetectionEnabled = false;
            tabBackground.canvasGroup.blocksRaycasts = false;
        }
        else
        {
            // Enter
            tabBackground.Enter();
            DOFade(action.icon, 1f, animationDuration);
            
            action.tabIndicator.enterFrom = enterTransition;
            action.tabIndicator.Enter();
            actions.ForEach(it =>
            {
                if (it.index != action.index)
                {
                    DOFade(it.icon, 0.3f, animationDuration);
                    it.tabIndicator.leaveTo = leaveTransition;
                    it.tabIndicator.Leave();
                }
            });
            tabs[action.index].canvasGroup.blocksRaycasts = true;
            tabs[action.index].Enter();
            for (var index = 0; index < tabs.Count; index++)
            {
                if (index != action.index)
                {
                    tabs[index].canvasGroup.blocksRaycasts = false;
                    tabs[index].Leave();
                }
            }
            
            DOFade(closeAction.icon, 0.3f, animationDuration);

            closeDetectionArea.DetectionEnabled = true;
            tabBackground.canvasGroup.blocksRaycasts = true;
        }
        currentActionIndex = action.index;
        OnTabChanged.Invoke(prev, action);
    }
    
    public class Action : InteractableMonoBehavior
    {
        public ActionTabs owner;
        public int index;
        public Transform icon;
        public TransitionElement tabIndicator;
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            icon.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            icon.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            owner.OnAction(this);
        }
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (currentActionIndex >= 0) Close();
    }

    public void OnScreenChangeFinished(Screen from, Screen to) => Expression.Empty();

    private void DOFade(Transform transform, float target, float duration)
    {
        var image = transform.GetComponent<Image>();
        if (image)
        {
            image.DOFade(target, duration);
        }
        else
        {
            transform.GetComponent<CanvasGroup>().DOFade(target, duration);
        }
    }
}

public class TabChangeEvent : UnityEvent<ActionTabs.Action, ActionTabs.Action>
{
}