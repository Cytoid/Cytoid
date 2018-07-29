using System.Collections;
using System.Collections.Generic;
using Cytus2.Controllers;
using UnityEngine;
using UnityEngine.UI;

public class PauseButtonAnim : MonoBehaviour
{
    private CanvasGroup mainCanvas;
    private Image image;
    IEnumerator Start()
    {
        image = GetComponentInChildren<Image>();
        var arr = GetComponentsInChildren<CanvasGroup>();
        mainCanvas = arr[0];
        var canvas = arr[1];
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

    private void Update()
    {
        if (Game.Instance != null)
        {
            if (Game.Instance.IsCompleted || Game.Instance.IsFailed)
            {
                mainCanvas.alpha -= 0.0333f;
                image.raycastTarget = false;
            }
            else
            {
                mainCanvas.alpha = Game.Instance.IsPlaying ? 1 : 0;
                image.raycastTarget = Game.Instance.IsPlaying;
            }
        }
    }
}