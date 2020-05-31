using DG.Tweening;
using MoreMountains.NiceVibrations;
using UnityEngine.EventSystems;

public class AvatarNavigation : InteractableMonoBehavior
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (!Context.OnlinePlayer.IsAuthenticating)
        {
            Context.Haptic(HapticTypes.SoftImpact, true);
            Context.AudioManager.Get("Navigate1").Play(ignoreDsp: true);
            Context.ScreenManager.ChangeScreen(
                Context.OnlinePlayer.IsAuthenticated ? ProfileScreen.Id : SignInScreen.Id, ScreenTransition.Out);
        }
        else
        {
            GetComponent<PulseElement>()?.Pulse();
        }
    }

}