using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class OldScannerView : SingletonMonoBehavior<OldScannerView>
{

    private GameController game;
    private LayoutController layout;
    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        game = GameController.Instance;
        layout = LayoutController.Instance;
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.position = new Vector3(transform.position.x, -9999, -1);
        transform.localScale = new Vector3(1000, layout.ScannerHeight, 1);
    }

    private void Start()
    {
        if (!PlayerPrefsExt.GetBool("show_scanner", defaultValue:true))
        {
            spriteRenderer.enabled = false;
        }
        spriteRenderer.sortingOrder = 30100;
    }

    private void Update()
    {
        if (game.IsPaused) return;
        float y;
        if (game.CurrentPage % 2 == 0) // -y
        {
            y = layout.PlayAreaHeight -
                layout.PlayAreaHeight * ((game.TimeElapsed + game.Chart.PageShift) % game.Chart.PageDuration / game.Chart.PageDuration);
        }
        else // +y
        {
            y = layout.PlayAreaHeight * ((game.TimeElapsed + game.Chart.PageShift) % game.Chart.PageDuration / game.Chart.PageDuration);
        }
        if (game.isInversed)
        {
            y = layout.PlayAreaHeight - y;
        }
        y += layout.PlayAreaVerticalMargin;
        if (!game.IsLoaded)
        {
            y = -9999;
        }
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        if (game.IsEnded)
        {
            var newColor = spriteRenderer.color;
            newColor.a -= 0.333f * Time.deltaTime;
            spriteRenderer.color = newColor;
        }
    }
    
}
