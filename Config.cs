using Newtonsoft.Json;
using TShockAPI;
using static AutoChallenge.Plugin;

namespace AutoChallenge;

public class Challenge
{
    [JsonProperty("挑战名称")]
    public string Name { get; set; } = "新手挑战";
    
    [JsonProperty("挑战类型")]
    public ChallengeType Type { get; set; } = ChallengeType.击杀怪物;
    
    [JsonProperty("目标")]
    public string Target { get; set; } = "史莱姆";
    
    [JsonProperty("数量要求")]
    public int RequiredCount { get; set; } = 10;
    
    [JsonProperty("持续时间(秒)")]
    public int Duration { get; set; } = 300;
    
    [JsonProperty("奖励")]
    public Reward Reward { get; set; } = new();

    [JsonProperty("进度条件")]
    public string ProgressCondition { get; set; } = "";
}

public class Reward
{
    [JsonProperty("奖励类型")]
    public string Type { get; set; } = "物品";
    
    [JsonProperty("标识符")]
    public string Identifier { get; set; } = "71";
    
    [JsonProperty("数量")]
    public int Quantity { get; set; } = 1;
}

public class Config
{
    [JsonProperty("插件开关")]
    public bool Enabled { get; set; } = true;
    
    [JsonProperty("挑战间隔(秒)")]
    public int IntervalSeconds { get; set; } = 600;
    
    [JsonProperty("挑战列表")]
    public List<Challenge> Challenges { get; set; } = new();

    // 默认挑战示例（用于首次生成或合并新内容）
    private static List<Challenge> GetDefaultChallenges()
    {
        return new List<Challenge>
        {
            new Challenge
            {
                Name = "史莱姆猎人",
                Type = ChallengeType.击杀怪物,
                Target = "史莱姆",
                RequiredCount = 10,
                Duration = 300,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 5 },
                ProgressCondition = "肉前"
            },
            new Challenge
            {
                Name = "矿工新手",
                Type = ChallengeType.收集物品,
                Target = "铜矿",
                RequiredCount = 30,
                Duration = 600,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 8 },
                ProgressCondition = "肉前"
            },
            new Challenge
            {
                Name = "生存入门",
                Type = ChallengeType.生存时间,
                Target = "生存",
                RequiredCount = 60,
                Duration = 120,
                Reward = new Reward { Type = "Buff", Identifier = "5", Quantity = 1 },
                ProgressCondition = "肉前"
            },
            new Challenge
            {
                Name = "钓鱼新手",
                Type = ChallengeType.钓鱼,
                Target = "任何",
                RequiredCount = 5,
                Duration = 600,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 3 },
                ProgressCondition = "肉前"
            },
            new Challenge
            {
                Name = "机械猎手",
                Type = ChallengeType.击杀怪物,
                Target = "毁灭者",
                RequiredCount = 1,
                Duration = 900,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 50 },
                ProgressCondition = "肉山"
            },
            new Challenge
            {
                Name = "魂之收集者",
                Type = ChallengeType.收集物品,
                Target = "光明之魂",
                RequiredCount = 15,
                Duration = 800,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 30 },
                ProgressCondition = "肉山"
            },
            new Challenge
            {
                Name = "生存专家",
                Type = ChallengeType.生存时间,
                Target = "生存",
                RequiredCount = 300,
                Duration = 360,
                Reward = new Reward { Type = "Buff", Identifier = "11", Quantity = 1 },
                ProgressCondition = "肉山"
            },
            new Challenge
            {
                Name = "稀有鱼",
                Type = ChallengeType.钓鱼,
                Target = "金鲤鱼",
                RequiredCount = 1,
                Duration = 900,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 40 },
                ProgressCondition = "肉山"
            },
            new Challenge
            {
                Name = "四柱清扫",
                Type = ChallengeType.击杀怪物,
                Target = "日耀柱",
                RequiredCount = 1,
                Duration = 1200,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 100 },
                ProgressCondition = "月总"
            },
            new Challenge
            {
                Name = "夜明矿工",
                Type = ChallengeType.收集物品,
                Target = "夜明矿",
                RequiredCount = 20,
                Duration = 1000,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 80 },
                ProgressCondition = "月总"
            },
            new Challenge
            {
                Name = "生存大师",
                Type = ChallengeType.生存时间,
                Target = "生存",
                RequiredCount = 600,
                Duration = 720,
                Reward = new Reward { Type = "Buff", Identifier = "115", Quantity = 1 },
                ProgressCondition = "月总"
            },
            new Challenge
            {
                Name = "终极渔夫",
                Type = ChallengeType.钓鱼,
                Target = "普鲁姆",
                RequiredCount = 1,
                Duration = 1000,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 120 },
                ProgressCondition = "月总"
            },
            new Challenge
            {
                Name = "血月之夜",
                Type = ChallengeType.击杀怪物,
                Target = "滴滴怪",
                RequiredCount = 25,
                Duration = 500,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 25 },
                ProgressCondition = "血月"
            },
            new Challenge
            {
                Name = "建筑大师",
                Type = ChallengeType.收集物品,
                Target = "石块",
                RequiredCount = 300,
                Duration = 1200,
                Reward = new Reward { Type = "物品", Identifier = "71", Quantity = 15 },
                ProgressCondition = ""
            }
        };
    }

    public void Write()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
        TShock.Log.ConsoleInfo($"[AutoChallenge] 配置文件已保存: {ConfigPath}");
    }

    public static Config Read()
    {
        if (!File.Exists(ConfigPath))
        {
            TShock.Log.ConsoleInfo("[AutoChallenge] 配置文件不存在，正在创建默认配置...");
            var config = new Config();
            config.Challenges = GetDefaultChallenges();
            config.Write();
            return config;
        }
        
        try
        {
            string json = File.ReadAllText(ConfigPath);
            var config = JsonConvert.DeserializeObject<Config>(json);
            if (config != null)
            {
                // 合并默认挑战（仅当用户配置中没有同名的挑战时追加）
                var defaultChallenges = GetDefaultChallenges();
                var existingNames = new HashSet<string>(config.Challenges.Select(c => c.Name));
                int addedCount = 0;
                foreach (var def in defaultChallenges)
                {
                    if (!existingNames.Contains(def.Name))
                    {
                        config.Challenges.Add(def);
                        addedCount++;
                        TShock.Log.ConsoleInfo($"[AutoChallenge] 发现新挑战 \"{def.Name}\"，已自动添加到配置末尾");
                    }
                }

                if (addedCount > 0)
                {
                    TShock.Log.ConsoleInfo($"[AutoChallenge] 检测到新挑战，已自动添加 {addedCount} 条到配置文件末尾");
                    // 保存更新后的配置
                    config.Write();
                }
                else
                {
                    TShock.Log.ConsoleInfo("[AutoChallenge] 没有发现新挑战，配置无需更新");
                }

                TShock.Log.ConsoleInfo($"[AutoChallenge] 成功读取配置，加载 {config.Challenges.Count} 个挑战");
                return config;
            }
            
            TShock.Log.ConsoleError("[AutoChallenge] 配置文件解析失败，将使用默认配置");
            var fallbackConfig = new Config();
            fallbackConfig.Challenges = GetDefaultChallenges();
            fallbackConfig.Write();
            return fallbackConfig;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[AutoChallenge] 读取配置文件异常：{ex.Message}");
            var fallbackConfig = new Config();
            fallbackConfig.Challenges = GetDefaultChallenges();
            fallbackConfig.Write();
            return fallbackConfig;
        }
    }
}