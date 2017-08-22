using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ScannerView : SingletonMonoBehavior<ScannerView>
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
    }

    private void Update()
    {
        if (game.IsPaused) return;
        float y;
        if (game.CurrentPage % 2 == 0) // -y
        {
            y = layout.PlayAreaHeight -
                layout.PlayAreaHeight * ((game.TimeElapsed + game.Chart.pageShift) % game.Chart.pageDuration / game.Chart.pageDuration);
        }
        else // +y
        {
            y = layout.PlayAreaHeight * ((game.TimeElapsed + game.Chart.pageShift) % game.Chart.pageDuration / game.Chart.pageDuration);
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
