using UnityEngine;
using UnityEngine.UI;

public class GameProgressIndicator : MonoBehaviour
{
    public Image image;

    public Game game;

    private CanvasScaler canvasScaler;
    private RectTransform parentRectTransform;

    private void OnValidate()
    {
        this.AutoFill(ref image);
    }

    private void Awake()
    {
        this.AutoFill(ref image);
        canvasScaler = GetComponentInParent<CanvasScaler>();
        parentRectTransform = transform.parent as RectTransform;
        image.rectTransform.SetWidth(0);
        game.onGameUpdate.AddListener(OnGameUpdate);
    }

    private void OnGameUpdate(Game game)
    {
        var fullWidth = GetFullWidth();
        if (game.State.UseHealthSystem)
        {
            image.rectTransform.DOWidth((float) (fullWidth * game.State.Health / game.State.MaxHealth), 0.2f);
        }
        else
        {
            image.rectTransform.DOWidth(fullWidth * game.ChartProgress, 0.2f);
        }
    }

    private float GetFullWidth()
    {
        if (parentRectTransform != null && parentRectTransform.rect.width > 0)
        {
            return parentRectTransform.rect.width;
        }

        return canvasScaler != null ? canvasScaler.referenceResolution.x : image.rectTransform.rect.width;
    }
}
