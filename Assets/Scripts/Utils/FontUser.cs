using UnityEngine;
using UnityEngine.UI;

public class FontUser : MonoBehaviour
{
    private void Awake()
    {
        gameObject.GetComponentsInChildren<Text>(true).ForEach(Context.FontManager.UpdateText);
    }
}