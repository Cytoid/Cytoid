using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CytoidGameCoreEnvelope
{
    public const string CurrentSchema = "cytoid.game-core.v2";

    [JsonProperty("schema")]
    public string Schema { get; set; } = CurrentSchema;

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
            Schema = CurrentSchema,
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
