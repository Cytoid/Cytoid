public class SignInButton : SpinnerButton
{
    protected override async void OnClick()
    {
        base.OnClick();
        await ((SignInScreen) this.GetOwningScreen()).SignIn();
        IsSpinning = false;
    }
    
}