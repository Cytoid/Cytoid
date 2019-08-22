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
                tab.rectTransform.anchoredPosition = new Vector2(tab.rectTransform.anchoredPosition.x, 0);
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