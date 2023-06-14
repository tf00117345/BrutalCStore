using System.Text.Json.Serialization;

namespace BrutalCStore;

public class StoreConfig
{
    [JsonPropertyName("ip")] public string IP { get; set; }
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("calledAE")] public string CalledAe { get; set; }
    [JsonPropertyName("callingAE")] public string CallingAe { get; set; }
    [JsonPropertyName("countOfDcm")] public int CountOfDcm { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
}

public class DcmTagPair
{
    public string Name { get; set; }
    public string Group { get; set; }
    public string Element { get; set; }
    public string Value { get; set; }
}