using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class GradeText : TextBehavior
    {
        protected override void Awake()
        {
            base.Awake();
            
            var grade = ScoreGrades.From((float) CytoidApplication.CurrentPlay.Score);
            Text.text = "" + grade;
            Text.color = grade.Color();

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponentInParent<RectTransform>());
        }
    }
}