using System;
using System.IO;
using Newtonsoft.Json;

public class CompileStoryboardButton : InteractableMonoBehavior
{
    public PlayerGame game;

    private void Awake()
    {
        onPointerClick.AddListener(_ =>
        {
            var jObject = game.Storyboard.Compile();
            var path = game.Level.Path + "/storyboard_compiled.json";
            File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.None, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            }));
        });
    }
}