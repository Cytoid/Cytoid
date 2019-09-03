using UnityEngine.EventSystems;

public class RadioButton : InteractableMonoBehavior
{
    
    public RadioGroup radioGroup;
    public string value;

    public int Index => radioGroup.GetIndex(value);
    public bool IsSelected => radioGroup.IsSelected(this);
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Select();
    }
    
    public virtual void Select(bool pulse = true)
    {
        radioGroup.Value = value;
    }

    public virtual void Unselect()
    {
    }
    
}