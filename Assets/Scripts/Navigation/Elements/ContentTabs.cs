using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ContentTabs : LabelSelect
{
    
    public List<TransitionElement> tabs;
    public List<RectTransform> viewportContents;

    public ContentTabSelectEvent onTabSelect = new ContentTabSelectEvent();
    
    protected override void OnSelect(int newIndex)
    {
        base.OnSelect(newIndex);
        for (var index = 0; index < tabs.Count; index++)
        {
            var tab = tabs[index];
            if (index == newIndex)
            {
                viewportContents[index].anchoredPosition = new Vector2(viewportContents[index].anchoredPosition.x, 0);
                tab.enterDelay = 0.2f;
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

    public void UnselectAll()
    {
        SelectedIndex = -1;
        for (var index = 0; index < tabs.Count; index++)
        {
            var tab = tabs[index];
            tab.Leave(false, true);
            tab.canvasGroup.blocksRaycasts = false;
        }
    }
    
}

public class ContentTabSelectEvent : UnityEvent<int, TransitionElement>
{
}