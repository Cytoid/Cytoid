using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class TextBehavior : MonoBehaviour
    {

        protected Text Text;

        protected virtual void Awake()
        {
            Text = GetComponent<Text>();
        }
        
    }
}