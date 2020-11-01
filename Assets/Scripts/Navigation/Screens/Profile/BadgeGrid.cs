using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class BadgeGrid : MonoBehaviour
{

    public List<BadgeDisplay> badgeDisplays = new List<BadgeDisplay>();
    public GridLayoutGroup gridLayoutGroup;

    public void SetModel(IEnumerable<Badge> badges)
    {
        var list = badges.ToList();
        var rows = (int) Mathf.Ceil(list.Count / 4f);
        
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                var index = 4 * i + j;
                badgeDisplays[index].gameObject.SetActive(true);
                if (index < Math.Min(list.Count, badgeDisplays.Count))
                {
                    badgeDisplays[index].SetModel(list[index]);
                }
                else
                {
                    badgeDisplays[index].Clear();
                }
            }
        }
        for (var i = rows; i < badgeDisplays.Count / 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                badgeDisplays[4 * i + j].gameObject.SetActive(false);
            }
        }
    }
    
    
    
}