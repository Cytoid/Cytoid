using UniRx.Async;
using UnityEngine;

public class DragLineElement : MonoBehaviour
{
    private ChartModel.Note fromNoteModel;
    private ChartModel.Note toNoteModel;

    private float introRatio;
    private float outroRatio;

    private float start;
    private float end;
    private float length;

    private SpriteRenderer spriteRenderer;
    private Game game;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public async void SetData(Game game, ChartModel.Note fromNoteModel, ChartModel.Note toNoteModel)
    {
        this.game = game;
        this.fromNoteModel = fromNoteModel;
        this.toNoteModel = toNoteModel;
        spriteRenderer.material.SetFloat("_End", 0.0f);
        spriteRenderer.material.SetFloat("_Start", 0.0f);
        await UniTask.DelayFrame(0);
        length = Vector3.Distance(fromNoteModel.position, toNoteModel.position);
        spriteRenderer.material.mainTextureScale = new Vector2(1.0f, length / 0.16f);
        transform.position = fromNoteModel.position;
        transform.eulerAngles = new Vector3(0, 0, -fromNoteModel.rotation);
        transform.localScale = new Vector3(1.0f, length / 0.16f);
        spriteRenderer.sortingOrder = fromNoteModel.id;
    }

    private void Update()
    {
        spriteRenderer.enabled = !game.State.Mods.Contains(Mod.HideNotes);

        if (game is PlayerGame)
        {
            spriteRenderer.enabled = outroRatio < 1;
        }
        else
        {
            if (outroRatio >= 1)
            {
                Destroy(gameObject);
            }
        }

        if (game.Notes.ContainsKey(fromNoteModel.id))
        {
            var note = game.Notes[fromNoteModel.id];
            if (!note.IsCleared)
            {
                if (note.Renderer is ClassicNoteRenderer classicNoteRenderer)
                {
                    var fill = classicNoteRenderer.Fill;
                    spriteRenderer.color = spriteRenderer.color.WithAlpha(fill.enabled ? fill.color.a : 0);
                }
                else
                {
                    var f = 1 - note.TimeUntilStart / (note.Model.start_time - note.Model.intro_time);
                    f = Mathf.Clamp01(f);
                    spriteRenderer.color = Color.white.WithAlpha(f);
                }
            }
        }

        var time = game.Time;
        introRatio = (fromNoteModel.nextdraglinestoptime - time) /
                     (fromNoteModel.nextdraglinestoptime - fromNoteModel.nextdraglinestarttime);
        outroRatio = (time - fromNoteModel.start_time) / (toNoteModel.start_time - fromNoteModel.start_time);

        if (introRatio > 0 && introRatio < 1)
        {
            spriteRenderer.material.SetFloat("_End", 1.0f - introRatio);
        }
        else if (introRatio <= 0)
        {
            spriteRenderer.material.SetFloat("_End", 1.0f);
        }
        else
        {
            spriteRenderer.material.SetFloat("_End", 0.0f);
        }

        if (outroRatio > 0 && outroRatio < 1)
        {
            spriteRenderer.material.SetFloat("_Start", outroRatio);
        }
    }
}