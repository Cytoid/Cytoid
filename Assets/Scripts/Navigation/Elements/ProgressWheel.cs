using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ProgressWheel : MonoBehaviour
{
    [GetComponent] public Image image;

    private void Start()
    {
        image.rectTransform.localRotation = Quaternion.identity;
        
        image.rectTransform
            .DOLocalRotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
            .SetRelative()
            .SetEase(Ease.InOutExpo)
            .SetLoops(-1, LoopType.Incremental);
    }
}