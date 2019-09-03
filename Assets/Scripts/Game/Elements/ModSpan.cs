using UnityEngine;
using UnityEngine.UI;

public class ModSpan : MonoBehaviour
{
    public Game game;
    public LayoutGroup layoutGroup;

    protected void Awake()
    {
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
    }

    public void OnGameLoaded()
    {
        foreach (Transform child in layoutGroup.transform)
        {
            var pill = child.GetComponent<ModPill>();
            child.gameObject.SetActive(game.State.Mods.Contains(pill.mod));
        }
        layoutGroup.transform.RebuildLayout();
        GetComponentInParent<TransitionElement>().UseCurrentStateAsDefault();
    }
}