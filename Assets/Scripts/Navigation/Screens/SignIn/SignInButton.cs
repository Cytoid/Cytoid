public class SignInButton : SpinnerButton
{
    protected override async void OnClick()
    {
        base.OnClick();
        await ((SignInScreen) this.GetScreenParent()).SignIn();
        IsSpinning = false;
    }
    
}