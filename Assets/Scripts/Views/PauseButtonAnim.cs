using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseButtonAnim : MonoBehaviour
{
    IEnumerator Start()
    {
        var canvas = GetComponent<CanvasGroup>();
        if (PlayerPrefs.GetInt("pause-hint", 0) >= 3)
        {
            canvas.alpha = 0;
        }

        yield return new WaitForSeconds(3);
        while (canvas.alpha > 0)
        {
            canvas.alpha -= 0.01f;
            yield return new WaitForSeconds(0.02f);
        }

        var hintCount = PlayerPrefs.GetInt("pause-hint", 0) + 1;
        PlayerPrefs.SetInt("pause-hint", hintCount);
    }
}