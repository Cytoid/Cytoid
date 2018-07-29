using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class BestScoreText : TextBehavior
    {
        public static bool WillInvalidate = true;

        private void Update()
        {
            if (WillInvalidate)
            {
                if (LevelSelectionController.Instance.LoadedLevel == null) return;
                
                WillInvalidate = false;

                var ranked = PlayerPrefsExt.GetBool("ranked");
                var bestScore = ZPlayerPrefs.GetFloat(
                    PreferenceKeys.BestScore(LevelSelectionController.Instance.LoadedLevel.id,
                        CytoidApplication.CurrentChartType, ranked),
                    -1
                );

                if (Math.Abs(bestScore + 1) < 0.000001)
                {
                    Text.text = "No best score yet.";
                }
                else
                {
                    var bestAccuracy = ZPlayerPrefs.GetFloat(
                        PreferenceKeys.BestAccuracy(LevelSelectionController.Instance.LoadedLevel.id,
                            CytoidApplication.CurrentChartType, ranked)
                    );

                    var grade = ScoreGrades.From(bestScore);

                    Text.text = string.Format(
                        "<b><color=#{0}>{1} </color></b> {2:D6} / {3:0.00}%",
                        ColorUtility.ToHtmlStringRGB(grade.Color()),
                        grade.ToString(),
                        Mathf.FloorToInt(bestScore),
                        Math.Floor(bestAccuracy * 100) / 100
                    );
                }

                StartCoroutine(RebuildLayout());
            }
        }

        private IEnumerator RebuildLayout()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponentInParent<RectTransform>());
        }
    }
}