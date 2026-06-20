using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Splash : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    private void OnValidate()
    {
        this.AutoFill(ref canvasGroup);
    }

    private void Awake()
    {
        this.AutoFill(ref canvasGroup);
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