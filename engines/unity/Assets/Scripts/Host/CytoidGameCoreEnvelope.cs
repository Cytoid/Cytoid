using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CytoidGameCoreEnvelope
{
    [JsonProperty("v")]
    public int Version { get; set; } = 1;

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("payload")]
    public JToken Payload { get; set; } = new JObject();

    public static CytoidGameCoreEnvelope FromJson(string json)
    {
        return JsonConvert.DeserializeObject<CytoidGameCoreEnvelope>(json);
    }

    public static CytoidGameCoreEnvelope Create(string id, string type, JToken payload = null)
    {
        return new CytoidGameCoreEnvelope
        {
            Version = 1,
            Id = id,
            Type = type,
            Payload = payload ?? new JObject()
        };
    }

    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
