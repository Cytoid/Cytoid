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

}