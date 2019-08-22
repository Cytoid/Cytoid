using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Toast : SingletonMonoBehavior<Toast>
{
    public Color normalColor; // #728CE4
    public Color errorColor; // #DE3C4B
    public Image image; 
    public Text text;
    public Text stubText;

    public CanvasGroup canvasGroup;
    public CanvasGroup successIcon;
    public CanvasGroup spinnerIcon;
    public CanvasGroup failureIcon;

    private Dictionary<Status, CanvasGroup> icons = new Dictionary<Status, CanvasGroup>();
    private Queue<Entry> queue = new Queue<Entry>();
    private Entry currentEntry;

    private void Start()
    {
        icons[Status.Success] = successIcon;
        icons[Status.Loading] = spinnerIcon;
        icons[Status.Failure] = failureIcon;

        canvasGroup.alpha = 0;
        text.text = "";
        stubText.text = "";
        stubText.rectTransform.RebuildLayout();
        successIcon.gameObject.SetActive(true);
        failureIcon.gameObject.SetActive(true);
        spinnerIcon.gameObject.SetActive(true);
        successIcon.alpha = 0;
        spinnerIcon.alpha = 0;
        failureIcon.alpha = 0;

        var spinnerRectTransform = (RectTransform) spinnerIcon.transform;
        spinnerRectTransform.localRotation = Quaternion.identity;
        spinnerRectTransform
            .DOLocalRotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
            .SetRelative()
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Incremental);

        UpdateLoop();
    }

    public static void Enqueue(Status status, string message, float duration = 3f, bool transitive = false)
    {
        Instance.queue.Enqueue(new Entry {Status = status, Message = message, Duration = duration, Transitive = transitive});
    }
    
    public static void Next(Status status, string message, float duration = 3f, bool transitive = false)
    {
        Instance.queue.Clear();
        Enqueue(status, message, duration, transitive);
    }
    
    private async void UpdateLoop()
    {
        while (true)
        {
            if (queue.Count == 0)
            {
                if (canvasGroup.alpha.IsNotCloseTo(0))
                {
                    canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic).OnComplete(() =>
                    {
                        text.text = "";
                        stubText.text = "";
                        stubText.rectTransform.RebuildLayout();
                    });
                }
                await UniTask.DelayFrame(0);
                continue;
            }

            currentEntry = queue.Dequeue();

            if (canvasGroup.alpha.IsNotCloseTo(1))
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
                image.color = currentEntry.Status == Status.Failure ? errorColor : normalColor;
            }
            
            // Hide icon
            var iconToShow = icons[currentEntry.Status];
            foreach (var otherIcon in icons.Values)
                if (otherIcon != iconToShow && otherIcon.alpha > 0)
                {
                    print("Fading " + otherIcon.name);
                    otherIcon.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
                }


            if (!currentEntry.Transitive)
            {
                // Hide message
                text.DOFade(0, 0.2f).OnComplete(() => { text.text = currentEntry.Message; });
            }
            else
            {
                text.text = currentEntry.Message;
            }

            // Background color
            image.DOColor(currentEntry.Status == Status.Failure ? errorColor : normalColor, 0.4f).SetDelay(0.2f);
            
            // Layout group animation
            stubText.text = currentEntry.Message;
            stubText.rectTransform.RebuildLayout();
            
            // Show icon
            if (iconToShow.alpha < 1) iconToShow.DOFade(1, 0.2f).SetDelay(0.2f).SetEase(Ease.OutCubic);

            if (!currentEntry.Transitive)
            {
                // Show message
                text.DOFade(1, 0.2f).SetDelay(0.2f);
            }

            // Stall
            if (currentEntry.Duration > 0) await UniTask.Delay(TimeSpan.FromSeconds(currentEntry.Duration));
        }
    }

    public enum Status
    {
        Success,
        Loading,
        Failure
    }

    public class Entry
    {
        public Status Status;
        public string Message;
        public float Duration;
        public bool Transitive;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(Toast))]
public class ToastEditor : Editor
{
    private Toast.Status status = Toast.Status.Success;
    private string message = "This is a toast.";
    private float duration = 3f;
    private bool transitive;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Separator();

        status = (Toast.Status) EditorGUILayout.EnumPopup("Status", status);
        message = EditorGUILayout.TextField("Message", message);
        duration = EditorGUILayout.FloatField("Duration", duration);
        transitive = EditorGUILayout.Toggle("Transitive", transitive);
        if (GUILayout.Button("Push"))
        {
            Toast.Enqueue(status, message, duration, transitive);
        }
    }
}

#endif