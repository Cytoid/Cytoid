using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cytoid.Storyboard;
using Lean.Touch;
using UnityEngine;

public class GlobalCalibrator
{

    private readonly List<double> offsets = new List<double>();

    private readonly Game game;
    private readonly BeatPulseVisualizer beatPulseVisualizer;
    private readonly CircleProgressIndicator progressIndicator;
    private readonly GameMessageText messageText;

    private bool disposed;
    private bool needRetry;
    private int retries;
    private bool calibratedFourMeasures;
    private bool calibrationCompleted;

    private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();
    private readonly CancellationTokenSource canExitSource = new CancellationTokenSource();
    
    public GlobalCalibrator(Game game)
    {
        this.game = game;
        beatPulseVisualizer = GameObjectProvider.Instance.beatPulseVisualizer;
        progressIndicator = GameObjectProvider.Instance.circleProgressIndicator;
        messageText = GameObjectProvider.Instance.messageText;
        
        // Reset offset
        Context.Player.Settings.BaseNoteOffset = 0;
        Context.Player.SaveSettings();
        game.Level.Record.RelativeNoteOffset = 0;
        game.Level.SaveRecord();

        // Hide overlay UI
        StoryboardRendererProvider.Instance.UiCanvasGroup.alpha = 0;
        game.onGameStarted.AddListener(_ =>
        {
            game.Config.GlobalNoteOpacityMultiplier = 0;
            Flow();
            DetectCanSkipCalibration();
        });
        game.BeforeExitTasks.Add(UniTask.Never(canExitSource.Token)); // Game never switches scenes by itself
    }

    public void Restart()
    {
        game.Retry();
    }

    private async void Flow()
    {
        try
        {
            messageText.Enqueue("OFFSET_SETUP_WIZARD_1".Get());
            await UniTask.Delay(4000);

            messageText.Enqueue("OFFSET_SETUP_WIZARD_2".Get());
            LeanTouch.OnFingerDown = OnFingerDown;
            await UniTask.WaitUntil(() => needRetry || calibratedFourMeasures,
                cancellationToken: cancelSource.Token);

            reset:
            if (needRetry)
            {
                needRetry = false;
                messageText.Enqueue("OFFSET_SETUP_WIZARD_3".Get());
                await UniTask.WaitUntil(() => needRetry || calibratedFourMeasures, cancellationToken: cancelSource.Token);
                if (needRetry)
                {
                    calibratedFourMeasures = false;
                    goto reset;
                }
            }

            messageText.Enqueue("OFFSET_SETUP_WIZARD_4".Get());
            await UniTask.WaitUntil(() => needRetry || calibrationCompleted, cancellationToken: cancelSource.Token);
            
            if (needRetry)
            {
                calibratedFourMeasures = false;
                goto reset;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    
    private async void DetectCanSkipCalibration()
    {
        try
        {
            await UniTask.WhenAny(
                UniTask.WaitUntil(() => retries >= 10),
                UniTask.Delay(TimeSpan.FromSeconds(120), cancellationToken: cancelSource.Token)
            );
            AskSkipCalibration();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnFingerDown(LeanFinger finger)
    {
        var lastNote = game.Chart.Model.note_list.FindLast(it => it.start_time - 0.5f < game.Time);
        var error = game.Time - lastNote.start_time;

        game.effectController.PlayRippleEffect(finger.GetWorldPosition(0, game.camera));
        beatPulseVisualizer.StartPulsing();
        Debug.Log($"{calibratedFourMeasures} - Offset: {error}s");
        offsets.Add(error);
        if (offsets.Count > 1 && Math.Abs(offsets.Last() - offsets.GetRange(0, offsets.Count - 1).Average()) > 0.080)
        {
            retries++;
            needRetry = true;
            calibratedFourMeasures = false;
            offsets.Clear();
            progressIndicator.Progress = 0;
            progressIndicator.Text = "";
            return;
        }

        var progress = calibratedFourMeasures ? 4 + offsets.Count : offsets.Count;
        progressIndicator.Progress = progress * 1f / 8f;
        progressIndicator.Text = $"{progress} / 8";

        if (offsets.Count == 4)
        {
            if (calibratedFourMeasures)
            {
                LeanTouch.OnFingerDown = _ => { };
                PromptComplete();
            }
            else
            {
                calibratedFourMeasures = true;
                offsets.Clear();
            }
        }
    }

    private void AskSkipCalibration()
    {
        if (calibrationCompleted) return;
        
        LeanTouch.OnFingerDown = _ => { };
        game.Complete(true);
        Dialog.Prompt("OFFSET_SETUP_WIZARD_DIALOG_ASK_SKIP".Get(), Skip, Restart);
    }

    private void Skip()
    {
        if (calibrationCompleted) return;

        LeanTouch.OnFingerDown = _ => { };
        offsets.Clear();
        Complete();
    }

    private void PromptComplete()
    {
        if (calibrationCompleted) return;

        LeanTouch.OnFingerDown = _ => { };
        calibrationCompleted = true;
        game.Complete(true);
        Dialog.PromptAlert("OFFSET_SETUP_WIZARD_DIALOG_COMPLETE".Get($"{offsets.Average():F3}"),
            Complete
        );
    }

    private async void Complete()
    {
        LeanTouch.OnFingerDown = _ => { };
        calibrationCompleted = true;
        messageText.Enqueue(string.Empty, true);
        progressIndicator.Progress = 0;
        progressIndicator.Text = string.Empty;

        if (offsets.Count > 0)
        {
            Context.Player.Settings.BaseNoteOffset = (float) Math.Round((decimal) offsets.Average(), 3, MidpointRounding.AwayFromZero);
            Context.Player.SaveSettings();
        }

        canExitSource.Cancel();
        game.Complete(true);
    }

    public void Dispose()
    {
        if (disposed) return;
        LeanTouch.OnFingerDown = _ => { };
        disposed = true;
        cancelSource.Cancel();
    }


}