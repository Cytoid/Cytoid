using UnityEngine;

public class ParticleManager : SingletonMonoBehavior<ParticleManager>
{

    public ParticleSystem clearFX;
    public ParticleSystem clearChainFX;
    public ParticleSystem missFX;
    public ParticleSystem holdFX;

    private ThemeController theme;

    protected override void Awake()
    {
        base.Awake();
        theme = ThemeController.Instance;
    }

    public void PlayClearFX(OldNoteView noteView, NoteGrading grading, float timeUntilComplete, bool earlyLateIndicator)
    {
        var at = noteView.transform.position;
        var clearFX = this.clearFX;
        if (noteView is ChainNoteView)
        {
            clearFX = clearChainFX;
        }
        if (noteView.note.type == OldNoteType.Hold)
        {
            at = new Vector3(at.x, OldScannerView.Instance.transform.position.y, at.z);
        }
        if (grading == NoteGrading.Miss)
        {
            var fx = Instantiate(missFX, at, Quaternion.identity);
            fx.Stop();
            
            var mainModule = fx.main;
            mainModule.simulationSpeed = 0.3f;
            mainModule.duration = mainModule.duration / 0.3f;
            mainModule.startColor = theme.missColor;
            
            /*var childFx = fx.transform.GetChild(0).GetComponent<ParticleSystem>();
            var childMainModule = childFx.main;
            childMainModule.startColor = noteView.fillColor;*/

            if (noteView.note.type == OldNoteType.Chain)
                fx.transform.localScale = new Vector3(2, 2, 2);
            
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
        else
        {
            var fx = Instantiate(clearFX, at, Quaternion.identity);
            fx.Stop();

            if (!(noteView is ChainNoteView))
            {
                if (earlyLateIndicator)
                {
                    if (grading != NoteGrading.Perfect)
                    {
                        fx.transform.GetChild(0).GetChild(timeUntilComplete > 0 ? 1 : 0).gameObject.SetActive(false);
                        /*var system = fx.transform.GetChild(0).GetChild(timeUntilComplete > 0 ? 1 : 0).GetComponent<ParticleSystem>();
                        var colorOverLifetime = system.colorOverLifetime;
                        colorOverLifetime.color = new ParticleSystem.MinMaxGradient();*/
                    }
                    else
                    {
                        fx.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                        fx.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                    }
                }
                else
                {
                    fx.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                    fx.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                }
            }
            
            var speed = 1f;
            var color = theme.perfectColor;
            switch (grading)
            {
                case NoteGrading.Great:
                    speed = 0.9f;
                    color = theme.greatColor;
                    break;
                case NoteGrading.Good:
                    speed = 0.7f;
                    color = theme.goodColor;
                    break;
                case NoteGrading.Bad:
                    speed = 0.5f;
                    color = theme.badColor;
                    break;
            }
            
            var mainModule = fx.main;
            mainModule.simulationSpeed = speed;
            mainModule.duration = mainModule.duration / speed;
            mainModule.startColor = color;
            
            if (noteView.note.type == OldNoteType.Chain)
                fx.transform.localScale = new Vector3(3f, 3f, 3f);
            
            /*var childFx = fx.transform.GetChild(0).GetComponent<ParticleSystem>();
            var childMainModule = childFx.main;
            childMainModule.startColor = noteView.fillColor;*/
            
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
    }

    public void PlayHoldFX(OldNoteView noteView)
    {
        var fx = Instantiate(holdFX, noteView.transform);

        var newPos = fx.transform.position;
        newPos.z -= 0.2f;
        fx.transform.position = newPos;
        
        fx.Stop();
   
        var mainModule = fx.main;
        mainModule.startColor = noteView.ringColor;
            
        fx.Play();
        Destroy(fx.gameObject, fx.main.duration);
    }

}