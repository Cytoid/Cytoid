using System.Linq;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen
{
    
    public LoopVerticalScrollRect scrollRect;
    
    public override string GetId() => "LevelSelection";
    
    public override void OnScreenCreated()
    {
        print("filling...");
        scrollRect.totalCount = Context.levelManager.loadedLevels.Count;
        scrollRect.objectsToFill = Context.levelManager.loadedLevels.ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
    }

    public override void OnScreenUpdate()
    {
        
    }

    public override void OnScreenDestroyed()
    {
        Destroy(scrollRect);
        Context.spriteCache.Clear();
    }

}