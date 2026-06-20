using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class GameMessageText : SingletonMonoBehavior<GameMessageText>
{

    public Text text;
    public TransitionElement transitionElement;

    private readonly Queue<string> messages = new Queue<string>();

    protected override async void Awake()
    {
        base.Awake();
        while (this != null)
        {
            await UniTask.WaitUntil(() => this == null || messages.Count > 0);
            if (this == null) return;

            if (transitionElement.IsShown)
            {
                transitionElement.Leave();
                await UniTask.WaitUntil(() => !transitionElement.IsInTransition);
            }
            var message = messages.Dequeue();
            text.text = message;

            transitionElement.Enter();
            await UniTask.WaitUntil(() => !transitionElement.IsInTransition);
        }
    }

    public void Enqueue(string message, bool clearPrevious = false)
    {
        if (clearPrevious) messages.Clear();
        messages.Enqueue(message);
    }

}