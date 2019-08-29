using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ContentTabs : LabelSelect
{
    
    public List<TransitionElement> tabs;

    public ContentTabSelectEvent onTabSelect = new ContentTabSelectEvent();
    
    protected override void OnSelect(int newIndex)
    {
        base.OnSelect(newIndex);
        for (var index = 0; index < tabs.Count; index++)
        {
            var tab = tabs[index];
            if (index == newIndex)
            {
                var rect = tab.transform.Find("Viewport/Content").transform as RectTransform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0);
                tab.Enter(false);
                tab.canvasGroup.blocksRaycasts = true;
            }
            else
            {
                tab.Leave(false);
                tab.canvasGroup.blocksRaycasts = false;
            }
        }
        onTabSelect.Invoke(newIndex, tabs[newIndex]);
    }
    
}

public class ContentTabSelectEvent : UnityEvent<int, TransitionElement>
{
}