using UnityEngine;

public class NavigationUiElementProvider : SingletonMonoBehavior<NavigationUiElementProvider>
{
    public GameObject pillRadioButton;
    public GameObject toggleRadioButton;
    public InputPreferenceElement input;
    public PillRadioGroupPreferenceElement pillRadioGroup;
    public SelectPreferenceElement select;
    public ToggleRadioGroupPreferenceElement toggleRadioGroupVertical;
}