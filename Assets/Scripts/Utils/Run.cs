using UnityEngine;
using System.Collections;

public class Run
{
    public bool isDone;
    public bool abort;
    private IEnumerator action;

    #region Run.EachFrame

    public static Run EachFrame(System.Action aAction)
    {
        var tmp = new Run();
        tmp.action = _RunEachFrame(tmp, aAction);
        tmp.Start();
        return tmp;
    }

    private static IEnumerator _RunEachFrame(Run aRun, System.Action aAction)
    {
        aRun.isDone = false;
        while (true)
        {
            if (!aRun.abort && aAction != null)
                aAction();
            else
                break;
            yield return null;
        }

        aRun.isDone = true;
    }

    #endregion Run.EachFrame

    #region Run.Every

    public static Run Every(float aInitialDelay, float aDelay, System.Action aAction)
    {
        var tmp = new Run();
        tmp.action = _RunEvery(tmp, aInitialDelay, aDelay, aAction);
        tmp.Start();
        return tmp;
    }

    private static IEnumerator _RunEvery(Run aRun, float aInitialDelay, float aSeconds, System.Action aAction)
    {
        aRun.isDone = false;
        if (aInitialDelay > 0f)
            yield return new WaitForSeconds(aInitialDelay);
        else
        {
            var frameCount = Mathf.RoundToInt(-aInitialDelay);
            for (var i = 0; i < frameCount; i++)
                yield return null;
        }

        while (true)
        {
            if (!aRun.abort && aAction != null)
                aAction();
            else
                break;
            if (aSeconds > 0)
                yield return new WaitForSeconds(aSeconds);
            else
            {
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(-aSeconds));
                for (var i = 0; i < frameCount; i++)
                    yield return null;
            }
        }

        aRun.isDone = true;
    }

    #endregion Run.Every

    #region Run.After

    public static Run After(float aDelay, System.Action aAction)
    {
        var tmp = new Run();
        tmp.action = _RunAfter(tmp, aDelay, aAction);
        tmp.Start();
        return tmp;
    }

    private static IEnumerator _RunAfter(Run aRun, float aDelay, System.Action aAction)
    {
        aRun.isDone = false;
        yield return new WaitForSeconds(aDelay);
        if (!aRun.abort && aAction != null)
            aAction();
        aRun.isDone = true;
    }

    #endregion Run.After
   
    #region Run.Lerp

    public static Run Lerp(float aDuration, System.Action<float> aAction)
    {
        var tmp = new Run();
        tmp.action = _RunLerp(tmp, aDuration, aAction);
        tmp.Start();
        return tmp;
    }

    private static IEnumerator _RunLerp(Run aRun, float aDuration, System.Action<float> aAction)
    {
        aRun.isDone = false;
        var t = 0f;
        while (t < 1.0f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / aDuration);
            if (!aRun.abort && aAction != null)
                aAction(t);
            yield return null;
        }

        aRun.isDone = true;
    }

    #endregion Run.Lerp 

    private void Start()
    {
        if (action != null)
            Context.Instance.StartCoroutine(action);
    }

    public Coroutine WaitFor
    {
        get { return Context.Instance.StartCoroutine(_WaitFor(null)); }
    }

    public IEnumerator _WaitFor(System.Action aOnDone)
    {
        while (!isDone)
            yield return null;
        if (aOnDone != null)
            aOnDone();
    }

    public void Abort()
    {
        abort = true;
    }

    public Run ExecuteWhenDone(System.Action aAction)
    {
        var tmp = new Run();
        tmp.action = _WaitFor(aAction);
        tmp.Start();
        return tmp;
    }
}