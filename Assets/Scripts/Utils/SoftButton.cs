using UnityEngine.UI;

public class SoftButton : InteractableMonoBehavior
{
    public TransitionElement transitionElement;
    public Text text;

    public void SetText(string text)
    {
        this.text.text = text;
    }
}