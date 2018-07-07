using Cytus2.Views;

namespace Cytus2.Models
{

    public class ClickNote : GameNote
    {

        protected override void Awake()
        {
            base.Awake();
            View = new ClickNoteView(this);
        }

    }

}