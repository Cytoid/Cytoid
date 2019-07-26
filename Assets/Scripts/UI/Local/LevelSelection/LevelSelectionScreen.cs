using System.Linq;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen
{
    
    public LoopVerticalScrollRect scrollRect;
    
    public override string GetId() => "LevelSelection";
    
    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        scrollRect.totalCount = Context.levelManager.loadedLevels.Count;
        scrollRect.objectsToFill = Context.levelManager.loadedLevels.ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        
        Destroy(scrollRect);
        Context.spriteCache.Clear();
    }

}