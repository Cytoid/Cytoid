using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Splash : MonoBehaviour
{
    [GetComponent] public CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup.alpha = 0;
    }

    public async UniTask Display()
    {
        canvasGroup.DOFade(1, 1);
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        canvasGroup.DOFade(0, 1);
        await UniTask.Delay(TimeSpan.FromSeconds(1));
    }
}