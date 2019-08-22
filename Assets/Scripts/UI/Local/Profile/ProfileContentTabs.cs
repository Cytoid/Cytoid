public class ProfileContentTabs : ContentTabs
{
    public TransitionElement character;
    
    protected override void OnSelect(int newIndex)
    {
        base.OnSelect(newIndex);
        if (newIndex == 0)
        {
            character.Enter();
        }
        else
        {
            character.Leave(false);
        }
    }
}