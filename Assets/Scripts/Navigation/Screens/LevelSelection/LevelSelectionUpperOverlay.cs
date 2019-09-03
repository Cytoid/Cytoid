using UnityEngine.UI;

public class LevelSelectionUpperOverlay : UpperOverlay
{
    public LoopScrollRect loopScrollRect;
    
    protected override void Update()
    {
        if (loopScrollRect.StartItemIndex > 0)
        {
            canvasGroup.alpha = maxAlpha;
        }
        else
        {
            base.Update();
        }
    }
}