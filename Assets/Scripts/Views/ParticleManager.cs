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

    public void PlayClearFX(NoteView noteView, NoteRanking ranking)
    {
        var at = noteView.transform.position;
        var clearFX = this.clearFX;
        if (noteView is ChainNoteView)
        {
            clearFX = clearChainFX;
        }
        if (noteView.note.type == NoteType.Hold)
        {
            at = new Vector3(at.x, ScannerView.Instance.transform.position.y, at.z);
        }
        if (ranking == NoteRanking.Miss)
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

            if (noteView.note.type == NoteType.Chain)
                fx.transform.localScale = new Vector3(2, 2, 2);
            
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
        else
        {
            var fx = Instantiate(clearFX, at, Quaternion.identity);
            fx.Stop();
            
            var speed = 1f;
            var color = theme.perfectColor;
            switch (ranking)
            {
                case NoteRanking.Excellent:
                    speed = 0.9f;
                    color = theme.excellentColor;
                    break;
                case NoteRanking.Good:
                    speed = 0.7f;
                    color = theme.goodColor;
                    break;
                case NoteRanking.Bad:
                    speed = 0.5f;
                    color = theme.badColor;
                    break;
            }
            
            var mainModule = fx.main;
            mainModule.simulationSpeed = speed;
            mainModule.duration = mainModule.duration / speed;
            mainModule.startColor = color;
            
            if (noteView.note.type == NoteType.Chain)
                fx.transform.localScale = new Vector3(3f, 3f, 3f);
            
            /*var childFx = fx.transform.GetChild(0).GetComponent<ParticleSystem>();
            var childMainModule = childFx.main;
            childMainModule.startColor = noteView.fillColor;*/
            
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
    }

    public void PlayHoldFX(NoteView noteView)
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