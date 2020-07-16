using Cysharp.Threading.Tasks;

public class PromiseTask<T>
{
    public T Result { get; private set; }
    public bool IsDone { get; private set; }
    
    public UniTask<T>.Awaiter GetAwaiter()
    {
        return UniTask.WaitUntil(() => IsDone).ContinueWith(() => Result).GetAwaiter();
    }

    public void Resolve(T result)
    {
        IsDone = true;
        Result = result;
    }

    public void Reject()
    {
        IsDone = true;
    }
}