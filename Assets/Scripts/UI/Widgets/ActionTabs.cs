using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionTabs : MonoBehaviour
{
    
    private float animationDuration = 0.4f;
    
    public List<GameObject> icons;
    public List<TransitionElement> tabs;
    public TransitionElement tabBackground;
    public RectangularDetectionArea closeDetectionArea;
    
    private Action closeAction;
    private List<Action> actions = new List<Action>();
    private int currentActionIndex;
    
    private void Awake()
    {
        // Make sure game objects are active
        icons.ForEach(it => it.gameObject.SetActive(true));
        tabs.ForEach(it => it.gameObject.SetActive(true));
        tabBackground.gameObject.SetActive(true);
        
        // Setup close button
        var closeIcon = gameObject.transform.Find("Close");
        closeAction = closeIcon.gameObject.AddComponent<Action>();
        closeAction.owner = this;
        closeAction.index = -1;
        closeAction.icon = closeIcon.Find("Icon").GetComponent<Image>();
        closeAction.icon.DOFade(0, 0);

        // Setup actions and tabs
        for (var index = 0; index < icons.Count; index++)
        {
            var actionGameObject = icons[index];
            var icon = actionGameObject.transform.Find("Icon");
            var tabIndicator = actionGameObject.transform.Find("TabIndicator");
            if (icon == null || tabIndicator == null) continue;

            var action = actionGameObject.AddComponent<Action>();
            action.owner = this;
            action.index = index;
            action.icon = icon.GetComponent<Image>();
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

    public void Close()
    {
        OnAction(closeAction);
    }

    public void OnAction(Action action)
    {
        var enterTransition = action.index < currentActionIndex ? Transition.Right : Transition.Left;
        var leaveTransition = action.index < currentActionIndex ? Transition.Left : Transition.Right;
        if (action.index == -1)
        {
            // Close
            tabBackground.Leave();
            actions.ForEach(it =>
            {
                it.icon.DOFade(1f, animationDuration);
                it.tabIndicator.leaveTo = leaveTransition;
                it.tabIndicator.Leave();
            });
            tabs.ForEach(it =>
            {
                it.leaveTo = Transition.Right; // TODO: Customize?
                it.Leave();
            });

            closeAction.icon.DOFade(0, animationDuration);
            
            closeDetectionArea.DetectionEnabled = false;
            tabBackground.canvasGroup.blocksRaycasts = false;
        }
        else
        {
            // Enter
            tabBackground.Enter();
            action.icon.DOFade(1f, animationDuration);
            
            action.tabIndicator.enterFrom = enterTransition;
            action.tabIndicator.Enter();
            actions.ForEach(it =>
            {
                if (it.index != action.index)
                {
                    it.icon.DOFade(0.3f, animationDuration);
                    it.tabIndicator.leaveTo = leaveTransition;
                    it.tabIndicator.Leave();
                }
            });
            tabs[action.index].Enter();
            for (var index = 0; index < tabs.Count; index++)
            {
                if (index != action.index) tabs[index].Leave();
            }
            
            closeAction.icon.DOFade(0.3f, animationDuration);

            closeDetectionArea.DetectionEnabled = true;
            tabBackground.canvasGroup.blocksRaycasts = true;
        }
        currentActionIndex = action.index;
    }
    
    public class Action : InteractableMonoBehavior
    {
        public ActionTabs owner;
        public int index;
        public Image icon;
        public TransitionElement tabIndicator;
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            icon.transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            icon.transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            owner.OnAction(this);
        }
    }
}