using Newtonsoft.Json;
using static AutoChallenge.Plugin;

namespace AutoChallenge;

public class Data   // 必须为 public 或 internal（但同命名空间下 internal 也可）
{
    [JsonProperty("下一次挑战时间")]
    public DateTime NextChallengeTime { get; set; } = DateTime.UtcNow;
    
    [JsonProperty("当前挑战")]
    public string CurrentChallenge { get; set; } = "";
    
    [JsonProperty("总挑战次数")]
    public int TotalChallenges { get; set; } = 0;
    
    [JsonProperty("玩家完成记录")]
    public Dictionary<string, int> PlayerCompletions { get; set; } = new();
    
    [JsonProperty("玩家奖励记录")]
    public Dictionary<string, List<string>> PlayerRewards { get; set; } = new();

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(DataPath, json);
    }

    public static Data Read()
    {
        if (!File.Exists(DataPath))
        {
            var data = new Data();
            data.Save();
            return data;
        }
        
        string json = File.ReadAllText(DataPath);
        return JsonConvert.DeserializeObject<Data>(json) ?? new Data();
    }
}