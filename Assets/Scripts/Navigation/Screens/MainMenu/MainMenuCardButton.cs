using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuCardButton : NavigationElement
{
    public RectTransform parent;
    public bool requireOnline;
    public bool requireAuthentication;

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (requireOnline && Context.IsOffline())
        {
            Dialog.PromptAlert("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
            return;
        }

        if (requireAuthentication && !Context.OnlinePlayer.IsAuthenticated)
        {
            Toast.Next(Toast.Status.Failure, "TOAST_SIGN_IN_REQUIRED".Get());

            if (!Context.OnlinePlayer.IsAuthenticating)
            {
                Context.ScreenManager.ChangeScreen(SignInScreen.Id, ScreenTransition.Out);
            }
            return;
        }
        
        if (Context.ScreenManager.IsScreenCreated(targetScreenId))
        {
            transitionFocus = parent.GetScreenSpaceCenter();
            base.OnPointerClick(eventData);
        }
        else
        {
            var dialog = Dialog.Instantiate();
            dialog.UseNegativeButton = false;
            dialog.UsePositiveButton = true;
            dialog.Message = "DIALOG_COMING_SOON".Get();
            dialog.Open();
        }
    }
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        parent.DOScale(0.95f, 0.2f).SetEase(Ease.OutCubic);
    }
        
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        parent.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }
}