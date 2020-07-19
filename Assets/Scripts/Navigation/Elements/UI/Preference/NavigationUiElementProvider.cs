using UnityEngine;

public class NavigationUiElementProvider : SingletonMonoBehavior<NavigationUiElementProvider>
{
    public GameObject pillRadioButton;
    public GameObject toggleRadioButton;
    public ButtonPreferenceElement buttonPreferenceElement;
    public InputPreferenceElement inputPreferenceElement;
    public PillRadioGroupPreferenceElement pillRadioGroupPreferenceElement;
    public SelectPreferenceElement selectPreferenceElement;
    public ToggleRadioGroupPreferenceElement toggleRadioGroupPreferenceElement;
}