using System;
using System.Collections;
using Cytus.Models;
using UnityEngine;

public class OldNoteView : MonoBehaviour
{
    
    public Note note;
    public RankedPlayData.Note rankedNoteData = new RankedPlayData.Note();

    public Chart Chart;
    public int page;
    public float x;
    public float y;
    public float endY;
    public float size;

    public bool displayed;
    public bool cleared;

    public Color ringColor;
    public Color fillColor;

    public bool scanningUpwards;

    protected GameController game;
    protected LayoutController layout;
    protected ThemeController theme;
    protected ParticleManager particleManager;

    [HideInInspector] public SpriteRenderer ringSpriteRenderer;
    [HideInInspector] public SpriteRenderer fillSpriteRenderer;
    [HideInInspector] public CircleCollider2D circleCollider;

    protected AudioSource hitSoundSource;

    public float TimeUntil;

    public float TimeDiff;

    /** A value to measure the difference between current timing and the "perfect" timing **/
    public double GreatGradeWeight;
    
    public virtual float Size
    {
        get { return layout.NoteSize; }
    }

    public virtual void Init(Chart chart, Note note)
    {
        this.note = note;
        rankedNoteData.id = note.id;
        this.Chart = chart;
        page = (int) ((note.time + chart.PageShift) / chart.PageDuration);
        x = note.x * layout.PlayAreaWidth + layout.PlayAreaHorizontalMargin;

        var distance = layout.PlayAreaHeight * note.duration / chart.PageDuration;
        
        if (page % 2 == 0) // -y
        {
            ringColor = this is ChainNoteView ? theme.ringColor3 : theme.ringColor1;
            fillColor = this is ChainNoteView ? theme.fillColor3 : theme.fillColor1;
            y = layout.PlayAreaHeight -
                layout.PlayAreaHeight * ((note.time + chart.PageShift) % chart.PageDuration / chart.PageDuration);
            endY = y - distance;
            scanningUpwards = false;
        }
        else // +y
        {
            ringColor = this is ChainNoteView ? theme.ringColor4 : theme.ringColor2;
            fillColor = this is ChainNoteView ? theme.fillColor4 : theme.fillColor2;
            y = layout.PlayAreaHeight * ((note.time + chart.PageShift) % chart.PageDuration / chart.PageDuration);
            endY = y + distance;
            scanningUpwards = true;
        }
       
        if (game.isInversed)
        {
            y = layout.PlayAreaHeight - y;
            endY = layout.PlayAreaHeight - endY;
            scanningUpwards = !scanningUpwards;
        }
        
        y += layout.PlayAreaVerticalMargin;
        endY += layout.PlayAreaVerticalMargin;
        size = Size;
        
        ringSpriteRenderer.enabled = false;
        ringSpriteRenderer.color = ringColor;
        fillSpriteRenderer.enabled = false;
        fillSpriteRenderer.color = fillColor;
        transform.position = new Vector3(x, y, note.id / 100f);

        ringSpriteRenderer.sortingOrder = (chart.ChronologicalIds.Count - note.id) * 3;
        fillSpriteRenderer.sortingOrder = ringSpriteRenderer.sortingOrder - 1;

        TimeUntil = note.time;
        TimeDiff = Math.Abs(TimeUntil);
        MaxMissThreshold = Mathf.Max(0.300f, note.duration);
    }

    public virtual void OnAllNotesInitialized()
    {
        
    }

    protected virtual void Awake()
    {
        game = GameController.Instance;
        layout = LayoutController.Instance;
        theme = ThemeController.Instance;
        particleManager = ParticleManager.Instance;
        ringSpriteRenderer = GetComponent<SpriteRenderer>();
        ringSpriteRenderer.enabled = false;
        fillSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        fillSpriteRenderer.enabled = false;
        circleCollider = transform.GetComponent<CircleCollider2D>();
        circleCollider.enabled = false;
        circleCollider.radius = circleCollider.radius * game.hitboxMultiplier;
        displayed = false;
        cleared = false;
        hitSoundSource = GetComponent<AudioSource>();
        hitSoundSource.clip = CytoidApplication.CurrentHitSound.Clip;
    }

    protected bool playedEmergeAnim;
    protected bool playedOpaqueAnim;

    protected virtual void FixedUpdate()
    {
        if (cleared) return;

        // Check removable
        if (IsMissed())
        {
            Clear(NoteGrading.Miss);
        }

        // Display only if still not clear yet
        if (!cleared)
        {
            // Display only if coming next row
            if (TimeDiff <= Chart.PageDuration)
            {
                OnDisplay();
                if (game.autoPlay && TimeDiff < 0.025f)
                {
                    if (this is OldHoldNoteView) ((OldHoldNoteView) this).StartHolding(); 
                    else Touch(new Vector2(0, 0));
                }
            }
        }
    }

    public float TimeScaledSize
    {
        get
        {
            if (this is ChainNoteView || TimeUntil < 0) return size;
            float timeScaledSize = size - TimeDiff / Chart.PageDuration * size / (note.type == OldNoteType.Single ? 2 : 4);
            if (timeScaledSize > size) timeScaledSize = size;
            return timeScaledSize;
        }
    }

    private float MaxMissThreshold;

    protected virtual bool IsMissed()
    {
        return TimeUntil < -MaxMissThreshold;
    }

    protected virtual void OnDisplay()
    {
        // Flashing effect
        // TODO: #

        if (!displayed)
        {
            displayed = true;
            ringSpriteRenderer.enabled = true;
            fillSpriteRenderer.enabled = true;
            circleCollider.enabled = true;
        }

        if (page > game.CurrentPage)
        {
            if (!playedEmergeAnim)
            {
                playedEmergeAnim = true;
                ringSpriteRenderer.color = new Color(ringSpriteRenderer.color.r, ringSpriteRenderer.color.g,
                    ringSpriteRenderer.color.b, 0f);
                fillSpriteRenderer.color = new Color(fillSpriteRenderer.color.r, fillSpriteRenderer.color.g,
                    fillSpriteRenderer.color.b, 0f);
                StartCoroutine(EmergeAnim());
            }
        }
        
        if (game.CurrentPageUnfloored + 0.333 > page)
        {
            if (!playedOpaqueAnim)
            {
                playedOpaqueAnim = true;
                StopCoroutine("EmergeAnim");
                StartCoroutine(OpaqueAnim());
            }
        }

        transform.localScale = new Vector3(TimeScaledSize, TimeScaledSize, transform.localScale.z);

        float t;
        if (TimeUntil > 0) t = 1 - Mathf.Clamp(TimeDiff / Chart.PageDuration, 0f, 1f);
        else t = 1f;

        // Fill scale
        switch (note.type)
        {
            case OldNoteType.Single:
                var z = fillSpriteRenderer.transform.localScale.z;
                fillSpriteRenderer.transform.localScale =
                    Vector3.Lerp(new Vector3(0, 0, z), new Vector3(1, 1, z), t);
                break;
            case OldNoteType.Chain:
                z = fillSpriteRenderer.transform.localScale.z;
                fillSpriteRenderer.transform.localScale = new Vector3(0.8f, 0.8f, z);
                break;
        }
    }

    protected virtual IEnumerator EmergeAnim()
    {
        ringSpriteRenderer.color = new Color(ringSpriteRenderer.color.r, ringSpriteRenderer.color.g,
            ringSpriteRenderer.color.b, ringSpriteRenderer.color.a + 0.05f);
        fillSpriteRenderer.color = new Color(fillSpriteRenderer.color.r, fillSpriteRenderer.color.g,
            fillSpriteRenderer.color.b, fillSpriteRenderer.color.a + 0.05f);
        yield return new WaitForSeconds(0.01f);
        if (ringSpriteRenderer.color.a < 0.67f)
        {
            yield return StartCoroutine(EmergeAnim());
        }
        else
        {
            yield return null;
        }
    }

    protected virtual IEnumerator OpaqueAnim()
    {
        ringSpriteRenderer.color = new Color(ringSpriteRenderer.color.r, ringSpriteRenderer.color.g,
            ringSpriteRenderer.color.b, ringSpriteRenderer.color.a + 0.05f);
        fillSpriteRenderer.color = new Color(fillSpriteRenderer.color.r, fillSpriteRenderer.color.g,
            fillSpriteRenderer.color.b, fillSpriteRenderer.color.a + 0.05f);
        yield return new WaitForSeconds(0.01f);
        if (ringSpriteRenderer.color.a < 1f)
        {
            yield return StartCoroutine(OpaqueAnim());
        }
        else
        {
            yield return null;
        }
    }

    public virtual void Touch(Vector2 touchScreenPosition)
    {
        if (!game.IsLoaded || game.IsPaused) return;
        if (this is ChainNoteView && CalculateGrading() == NoteGrading.Undetermined) return;
        rankedNoteData.press_time = TimeExt.Millis();
        rankedNoteData.press_x = (int) touchScreenPosition.x;
        rankedNoteData.press_y = (int) touchScreenPosition.y;
        Clear(CalculateGrading());
        if (hitSoundSource.clip != null)
        {
            hitSoundSource.Play();
        }
    }

    public virtual NoteGrading CalculateGrading()
    {
        var grading = NoteGrading.Miss;
        var timeUntil = TimeUntil;
        if (game.IsRanked)
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.400f)
                {
                    grading = NoteGrading.Bad;
                }
                if (timeUntil < 0.200f)
                {
                    grading = NoteGrading.Good;
                }
                if (timeUntil < 0.070f)
                {
                    grading = NoteGrading.Great;
                }
                if (timeUntil <= 0.040f)
                {
                    grading = NoteGrading.Perfect;
                }
                if (grading == NoteGrading.Great) {
                    GreatGradeWeight = 1 - (timeUntil - 0.04) / (0.07 - 0.04);
                }
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.200f)
                {
                    grading = NoteGrading.Bad;
                }
                if (timePassed < 0.150f)
                {
                    grading = NoteGrading.Good;
                }
                if (timePassed < 0.070f)
                {
                    grading = NoteGrading.Great;
                }
                if (timePassed <= 0.040f)
                {
                    grading = NoteGrading.Perfect;
                }
                if (grading == NoteGrading.Great) {
                    GreatGradeWeight = 1 - (timePassed - 0.04) / (0.07 - 0.04);
                }
            }
        }
        else
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.800f)
                {
                    grading = NoteGrading.Bad;
                }
                if (timeUntil < 0.400f)
                {
                    grading = NoteGrading.Good;
                }
                if (timeUntil < 0.200f)
                {
                    grading = NoteGrading.Great;
                }
                if (timeUntil < 0.070f)
                {
                    grading = NoteGrading.Perfect;
                }
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.300f)
                {
                    grading = NoteGrading.Bad;
                }
                if (timePassed < 0.200f)
                {
                    grading = NoteGrading.Good;
                }
                if (timePassed < 0.150f)
                {
                    grading = NoteGrading.Great;
                }
                if (timePassed < 0.070f)
                {
                    grading = NoteGrading.Perfect;
                }
            }
        }
        return grading;
    }

    public virtual void Clear(NoteGrading grading)
    {
        cleared = true;
        // Add to play data
        ((PlayData) game.PlayData).Clear(this);
        particleManager.PlayClearFX(this, grading, this is OldHoldNoteView ? ((OldHoldNoteView) this).note.time + ((OldHoldNoteView) this).note.duration - game.TimeElapsed: TimeUntil, game.showEarlyLateIndicator);
        ringSpriteRenderer.enabled = false;
        fillSpriteRenderer.enabled = false;
        circleCollider.enabled = false;
        // Add to ranked play data
        game.RankedPlayData.notes.Add(rankedNoteData);
        StartCoroutine(DestroyLater());
    }

    public virtual IEnumerator DestroyLater()
    {
        yield return new WaitForSeconds(3);
        game.NoteViews.Remove(note.id);
        Destroy(gameObject);
    }

    public bool OverlapPoint(Vector2 pos)
    {
        return circleCollider.OverlapPoint(pos);
    }
    
}