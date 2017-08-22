using System;
using System.Linq;
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
        }
        GameObject.FindGameObjectsWithTag("BackgroundCanvas").Where(it => it.scene != scene).ToList()
            .ForEach(Object.Destroy);
        return background;
    }

    public static void PersistBackgroundCanvas()
    {
        var backgroundCanvas = GameObject.FindGameObjectWithTag("BackgroundCanvas");
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