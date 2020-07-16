using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ModSpan : MonoBehaviour
{
    public Game game;
    public LayoutGroup layoutGroup;
    public TransitionElement parentTransitionElement;

    protected void Awake()
    {
        if (game != null)
        {
            game.onGameLoaded.AddListener(async _ =>
            {
                await UniTask.DelayFrame(0);
                UpdateMods(game.State.Mods);
            });
        }
        else
        {
            UpdateMods(Context.SelectedMods);
        }
    }

    public void UpdateMods(HashSet<Mod> mods)
    {
        foreach (Transform child in layoutGroup.transform)
        {
            var pill = child.GetComponent<ModPill>();
            pill.isStatic = true;
            child.gameObject.SetActive(mods.Contains(pill.mod));
        }
        layoutGroup.transform.RebuildLayout();
        parentTransitionElement.UseCurrentStateAsDefault();
    }

}