using System;
using DG.Tweening;
using Proyecto26;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RateLevelDialog : Dialog
{
    public Transform ratingRoot;
    public Transform hitboxHolder;
    public Transform overlayHolder;

    public Sprite starSprite;
    public Sprite starHalfSprite;

    [HideInInspector] public LevelRatedEvent onLevelRated = new LevelRatedEvent();

    public int Rating
    {
        get => rating;
        set
        {
            rating = Mathf.Clamp(value, 1, 10);
            UpdateOverlay();
        }
    }

    private string levelId;
    private int rating;
    private Image[] overlayImages;
    private int enterCount;
    private RequestHelper request;

    protected override void Awake()
    {
        base.Awake();
        Message = "DIALOG_RATE_THIS_LEVEL".Get();
        UsePositiveButton = true;
        UseNegativeButton = true;
        UseProgress = false;
        overlayImages = overlayHolder.GetComponentsInChildren<Image>();
        var k = 1;
        foreach (var image in hitboxHolder.GetComponentsInChildren<Image>())
        {
            var interactable = image.gameObject.AddComponent<InteractableMonoBehavior>();
            var setRating = k;
            interactable.onPointerEnter.AddListener(it =>
            {
                Rating = setRating;
                enterCount++;
                Enlarge();
            });
            interactable.onPointerExit.AddListener(async it =>
            {
                enterCount--;
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
                if (enterCount == 0) Shrink();
            });
            k++;
        }

        OnPositiveButtonClicked = dialog =>
        {
            print($"Rated {Rating}/10");
            positiveButton.IsSpinning = true;
            OnPositiveButtonClicked = _ => { };
            RestClient.Post<LevelRating>(request = new RequestHelper
                {
                    Uri = $"{Context.ApiBaseUrl}/levels/{levelId}/ratings",
                    Headers = Context.OnlinePlayer.GetJwtAuthorizationHeaders(),
                    Body = new LevelRating
                    {
                        rating = rating
                    }
                })
                .Then(it =>
                {
                    onLevelRated.Invoke(it);
                    Toast.Enqueue(Toast.Status.Success, "TOAST_SUCCESSFULLY_RATED_LEVEL".Get());
                    dialog.Close();
                })
                .Catch(error =>
                {
                    if (request != null && request.IsAborted)
                    {
                        Toast.Enqueue(Toast.Status.Success, "TOAST_RATING_CANCELLED".Get());
                    }
                    else
                    {
                        Debug.LogError(error);
                        Toast.Next(Toast.Status.Failure, "TOAST_COULD_NOT_RATE_LEVEL".Get());
                    }

                    dialog.Close();
                });
        };
        OnNegativeButtonClicked = dialog =>
        {
            if (request == null)
            {
                dialog.Close();
            }
            else
            {
                request.Abort();
            }
        };
    }

    private void Enlarge()
    {
        ratingRoot.DOScale(1.05f, 0.2f);
    }

    private void Shrink()
    {
        ratingRoot.DOScale(1f, 0.2f);
    }

    private void UpdateOverlay()
    {
        for (var i = 0; i < rating / 2; i++)
        {
            overlayImages[i].sprite = starSprite;
            overlayImages[i].SetAlpha(1);
        }

        if (rating % 2 == 1)
        {
            overlayImages[rating / 2].sprite = starHalfSprite;
            overlayImages[rating / 2].SetAlpha(1);
        }

        for (var i = rating / 2 + rating % 2; i < 5; i++)
        {
            overlayImages[i].SetAlpha(0);
        }
    }

    public static RateLevelDialog Instantiate(string levelId, int userRating = -1)
    {
        var dialog = Instantiate(NavigationObjectProvider.Instance.rateLevelDialogPrefab,
            NavigationObjectProvider.Instance.dialogHolder, false);
        dialog.levelId = levelId;
        dialog.Rating = userRating > 0 ? userRating : 8;
        if (userRating > 0)
        {
            // dialog.Message = $"You rated {userRating / 2:0.#}/5.";
        }

        return dialog;
    }
}

public class LevelRatedEvent : UnityEvent<LevelRating>
{
}

#if UNITY_EDITOR

[CustomEditor(typeof(RateLevelDialog))]
public class RateLevelDialogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Sample Rate Level Dialog"))
        {
            RateLevelDialog.Instantiate("io.cytoid.glow_dance").Open();
        }
    }
}

#endif