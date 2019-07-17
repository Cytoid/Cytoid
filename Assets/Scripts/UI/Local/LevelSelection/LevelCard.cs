using UnityEngine;
using UnityEngine.UI;

public class LevelCard : MonoBehaviour
{
    
    public Image cover;
    public CanvasGroup difficultyBallGroup;
    
    public Text artist;
    public Text title;
    public Text titleLocalized;

    private Level level;
    
    public GameObject difficultyBallPrefab;

    public void ScrollCellContent(object levelObject)
    {
        level = (Level) levelObject;
       
        artist.text = level.meta.artist;
        title.text = level.meta.title;
        titleLocalized.text = level.meta.artist_localized;
        titleLocalized.gameObject.SetActive(!string.IsNullOrEmpty(level.meta.artist_localized));

        foreach (Transform child in difficultyBallGroup.transform)
            Destroy(child.gameObject);
        foreach (var chart in level.meta.charts)
        {
            var difficultyBall = Instantiate(difficultyBallPrefab, difficultyBallGroup.transform)
                .GetComponent<DifficultyBall>();
            difficultyBall.Initialize(Difficulty.Parse(chart.type), chart.difficulty);
        }

        GetComponentInChildren<VerticalLayoutGroup>().transform.RebuildLayout();

        LoadCover();
    }

    public async void LoadCover()
    {
        var path = "file://" + level.path + ".thumbnail";

        var sprite = await Context.spriteCache.GetSprite(path);
        cover.sprite = sprite;
        cover.GetComponent<AspectRatioFitter>().aspectRatio = sprite.texture.width * 1.0f / sprite.texture.height;
    }
    
}