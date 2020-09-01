public class SignInButton : SpinnerButton
{
    public bool isSignUp = false;
    
    protected override async void OnClick()
    {
        base.OnClick();
        var screen = (SignInScreen) this.GetScreenParent();
        await (isSignUp ? screen.SignUp() : screen.SignIn());
        IsSpinning = false;
    }
    
}