using UnityEngine;
using UnityEngine.UI;

public class MediumAvatarWithName : MonoBehaviour
{
    public RectTransform root;
    public Avatar avatar;
    public Text nameText;

    public AvatarAction action = AvatarAction.OpenProfile;
    
    public void SetModel(OnlineUser user)
    {
        avatar.SetModel(user);
        nameText.text = user.Uid;
        root.RebuildLayout();
    }
    
    public void Dispose()
    {
        avatar.Dispose();       
    }
    
}