using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class BackgroundImage : MonoBehaviour
    {

        private void Awake()
        {
            EventKit.Subscribe("level loaded", OnLevelLoaded);
            SceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnSceneChange(Scene prev, Scene next)
        {
            switch (next.name)
            {
                case "CytusGame":
                    transform.DOKill();
                    transform.localScale = Vector3.one;
                    break;
            }
        }

        private void OnDestroy()
        {
            EventKit.Unsubscribe("level loaded", OnLevelLoaded);
            SceneManager.activeSceneChanged -= OnSceneChange;
        }
        
        private void OnLevelLoaded()
        {
            transform.localScale = Vector3.one;
            transform.DOKill();
            transform.DOScale(1.04f, 3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        }
        
    }
}