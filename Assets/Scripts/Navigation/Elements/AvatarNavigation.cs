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

            if (Context.OnlinePlayer.IsAuthenticated)
            {
                Context.ScreenManager.ChangeScreen(ProfileScreen.Id, ScreenTransition.In,
                    payload: new ProfileScreen.Payload {Id = Context.OnlinePlayer.LastProfile.User.Id});
            }
            else
            {
                Context.ScreenManager.ChangeScreen(SignInScreen.Id, ScreenTransition.In);
            }
        }
        else
        {
            GetComponent<PulseElement>()?.Pulse();
        }
    }

}