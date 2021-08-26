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

    public Sprite levelActionOverlayDownloadIcon;
    public Sprite levelActionOverlayDownloadedIcon;
    public Sprite levelActionOverlayDeleteIcon;
    public GradientMeshEffect levelActionOverlayDownloadGradient;
    public GradientMeshEffect levelActionOverlayDownloadedGradient;
    public GradientMeshEffect levelActionOverlayDeleteGradient;

}