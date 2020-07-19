using UnityEngine.UI;

public class SoftButton : InteractableMonoBehavior
{
    public TransitionElement transitionElement;
    public Text text;

    public string Label
    {
        get => text.text;
        set => text.text = value;
    }
    
}