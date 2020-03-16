using System;

[Serializable]
public class CharacterMeta
{
    public string id;
    public string name;
    public string description;
    public Illustrator illustrator;
    public CharacterDesigner characterDesigner;
    
    [Serializable]
    public class Illustrator
    {
        public string name;
        public string url;
    }
    
    [Serializable]
    public class CharacterDesigner
    {
        public string name;
        public string url;
    }

    public OnlineLevel level;
    public string asset;
    public OnlineImageAsset thumbnail;
    
}