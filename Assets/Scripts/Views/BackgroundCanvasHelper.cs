using System.Linq;
using DoozyUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BackgroundCanvasHelper
{
    public static GameObject SetupBackgroundCanvas(Scene scene)
    {
        var background = GameObject.FindGameObjectWithTag("Background");
        if (background != null)
        {
            background.transform.SetParent(scene.FindInSceneGameObjectWithTag("BackgroundCanvas").transform);
            background.transform.SetAsFirstSibling();
            background.transform.localScale = Vector3.one;
            background.GetComponent<RectTransform>().ChangeLocalPosition(z: 0);
            var rectTransform = background.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            rectTransform.anchoredPosition = new Vector2(0, 0);
        }

        GameObject.FindGameObjectsWithTag("BackgroundCanvas").Where(it => it.scene != scene).ToList()
            .ForEach(Object.Destroy);
        return background;
    }

    public static void PersistBackgroundCanvas()
    {
        var backgroundCanvas = GameObject.FindGameObjectWithTag("BackgroundCanvas");
        UIManager.CanvasDatabase.Remove(backgroundCanvas.GetComponent<UICanvas>().canvasName);
        foreach (Transform childTransform in backgroundCanvas.transform)
        {
            if (!childTransform.CompareTag("Background"))
            {
                Object.Destroy(childTransform.gameObject);
            }
        }

        Object.DontDestroyOnLoad(backgroundCanvas);
    }
}