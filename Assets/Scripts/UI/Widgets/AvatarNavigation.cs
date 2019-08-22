using DG.Tweening;
using UnityEngine.EventSystems;

public class AvatarNavigation : InteractableMonoBehavior
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (!Context.OnlinePlayer.IsAuthenticating)
        {
            Context.ScreenManager.ChangeScreen(
                Context.OnlinePlayer.IsAuthenticated ? ProfileScreen.Id : SignInScreen.Id, ScreenTransition.Out);
        }
        else
        {
            GetComponent<PulseElement>()?.Pulse();
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
    }
        
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

}