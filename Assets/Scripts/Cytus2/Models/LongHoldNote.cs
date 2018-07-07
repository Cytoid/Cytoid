using Cytus2.Views;

namespace Cytus2.Models
{

    public class LongHoldNote : HoldNote
    {
        
        protected override void Awake()
        {
            base.Awake();
            View = new LongHoldNoteView(this);
        }

    }

}