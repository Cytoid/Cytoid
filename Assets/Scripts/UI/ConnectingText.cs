using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class ConnectingText : MonoBehaviour
    {
        public string Text;

        private Text text;
        private float nextUpdateTime;
        private int n = 1;

        public void Awake()
        {
            text = GetComponent<Text>();
        }

        private void Update()
        {
            if (Time.time > nextUpdateTime)
            {
                nextUpdateTime += 1;
                text.text = Text + new string('.', n);
                if (n == 3) n = 1;
            }
        }
        
    }
}