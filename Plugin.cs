using Terraria;
using TShockAPI;
using TShockAPI.Hooks;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;

namespace AutoChallenge;

public enum ChallengeType
{
    击杀怪物,
    收集物品,
    生存时间,
    钓鱼
}

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    #region 插件信息
    public static string PluginName => "AutoChallenge";
    public override string Name => PluginName;
    public override string Author => "淦";
    public override Version Version => new(2026, 4, 4, 0); // 版本号已更新为当前日期
    public override string Description => "定时发起小挑战，完成获得奖励（手动提交物品）";
    #endregion

    #region 文件路径
    public static readonly string MainPath = Path.Combine(TShock.SavePath, PluginName);
    public static readonly string ConfigPath = Path.Combine(MainPath, "配置.json");
    public static readonly string DataPath = Path.Combine(MainPath, "数据.json");
    #endregion

    #region 注册与释放
    public Plugin(Main game) : base(game) { }

    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
        
        Commands.ChatCommands.Add(new Command("challenge.admin", ManageChallenge, "挑战管理", "cm")
        {
            HelpText = "管理定时挑战：/cm start <编号> - 手动开始挑战 | /cm stop - 停止当前挑战 | /cm list - 查看挑战列表 | /cm reload - 重载配置"
        });
        Commands.ChatCommands.Add(new Command("challenge.join", JoinChallenge, "参与挑战", "cjoin")
        {
            HelpText = "参与当前进行的挑战"
        });
        Commands.ChatCommands.Add(new Command("challenge.join", SubmitItems, "提交物品", "csubmit")
        {
            HelpText = "提交当前手持的物品用于完成收集/钓鱼挑战"
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置与数据
    internal static Config Config = new();
    internal static Data Data = new();
    
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendSuccessMessage($"[{PluginName}] 配置重载完成，当前 {Config.Challenges.Count} 个挑战");
    }

    private static void LoadConfig()
    {
        if (!Directory.Exists(MainPath))
            Directory.CreateDirectory(MainPath);
        
        Config = Config.Read();
        Data = Data.Read();
        
        if (Config.Enabled && string.IsNullOrEmpty(Data.CurrentChallenge))
        {
            Data.NextChallengeTime = DateTime.UtcNow.AddSeconds(Config.IntervalSeconds);
            Data.Save();
        }
    }
    #endregion

    #region 挑战状态
    private bool IsChallengeActive = false;
    private Challenge CurrentChallenge = null;
    private Dictionary<string, HashSet<string>> Participants = new();
    private Dictionary<string, HashSet<string>> Completers = new();
    private Dictionary<string, int> Progress = new();
    private DateTime LastSurvivalCheck = DateTime.UtcNow;
    #endregion

    #region 游戏更新事件
    private void OnUpdate(EventArgs args)
    {
        if (!Config.Enabled) return;
        
        var now = DateTime.UtcNow;
        
        if (!IsChallengeActive && now >= Data.NextChallengeTime)
        {
            StartRandomChallenge();
        }
        
        if (IsChallengeActive && CurrentChallenge != null && now >= Data.NextChallengeTime.AddSeconds(CurrentChallenge.Duration))
        {
            EndChallenge(false);
        }
        
        if (IsChallengeActive && CurrentChallenge?.Type == ChallengeType.生存时间)
        {
            if ((now - LastSurvivalCheck).TotalSeconds >= 1)
            {
                CheckSurvivalChallenge();
                LastSurvivalCheck = now;
            }
        }
    }
    #endregion

    #region 开始随机挑战
    private void StartRandomChallenge()
    {
        if (Config.Challenges.Count == 0)
        {
            TShock.Log.ConsoleError("[AutoChallenge] 没有可用的挑战配置");
            return;
        }

        var availableChallenges = Config.Challenges
            .Where(c => string.IsNullOrEmpty(c.ProgressCondition) || Utils.CheckCond(c.ProgressCondition, null))
            .ToList();

        if (availableChallenges.Count == 0)
        {
            TShock.Log.ConsoleInfo("[AutoChallenge] 当前没有符合条件的挑战，跳过本次触发");
            return;
        }

        var random = new Random();
        var challenge = availableChallenges[random.Next(availableChallenges.Count)];
        StartChallenge(challenge);
    }

    private void StartChallenge(Challenge challenge)
    {
        CurrentChallenge = challenge;
        IsChallengeActive = true;
        Participants[challenge.Name] = new HashSet<string>();
        Completers[challenge.Name] = new HashSet<string>();
        Progress.Clear();
        
        Data.NextChallengeTime = DateTime.UtcNow.AddSeconds(challenge.Duration);
        
        string msg = Utils.TextGradient($@"
╔══════════════════════════════════╗
║          新挑战开始          ║
╠══════════════════════════════════╣
║ 挑战名称：{challenge.Name}
║ 挑战类型：{challenge.Type}
║ 目标：{challenge.Target}
║ 数量要求：{challenge.RequiredCount}
║ 持续时间：{challenge.Duration / 60}分钟
║ 参与方式：输入 /cjoin 参与挑战
║ 完成方式：
║   - 击杀怪物/生存时间自动完成
║   - 收集物品/钓鱼需手持目标输入 /csubmit 提交
╠══════════════════════════════════╣
║ 完成奖励：{challenge.Reward.Type} ×{challenge.Reward.Quantity}
╚══════════════════════════════════╝");
        
        TSPlayer.All.SendMessage(msg, Color.Gold);
        
        Data.Save();
    }
    #endregion

    #region 结束挑战
    private void EndChallenge(bool completed)
    {
        if (CurrentChallenge == null) return;
        
        IsChallengeActive = false;
        
        if (completed)
        {
            string completeMsg = Utils.TextGradient($@"
╔══════════════════════════════════╗
║          挑战完成           ║
╠══════════════════════════════════╣
║ 挑战：{CurrentChallenge.Name}
║ 完成人数：{Completers[CurrentChallenge.Name].Count}
║ 奖励已发放！
╚══════════════════════════════════╝");
            
            TSPlayer.All.SendMessage(completeMsg, Color.Green);
            
            Data.TotalChallenges++;
            Data.Save();
        }
        else
        {
            TSPlayer.All.SendMessage($"[c/FF5555:挑战 {CurrentChallenge.Name} 超时结束，未完成的玩家无法获得奖励]", Color.Red);
        }
        
        Data.NextChallengeTime = DateTime.UtcNow.AddSeconds(Config.IntervalSeconds);
        Data.Save();
        
        CurrentChallenge = null;
    }
    #endregion

    #region 玩家参与挑战
    private void JoinChallenge(CommandArgs args)
    {
        if (!Config.Enabled)
        {
            args.Player.SendErrorMessage("插件未启用");
            return;
        }
        
        if (!IsChallengeActive || CurrentChallenge == null)
        {
            args.Player.SendErrorMessage("当前没有进行中的挑战");
            return;
        }
        
        var playerName = args.Player.Name;
        
        if (Participants[CurrentChallenge.Name].Contains(playerName))
        {
            args.Player.SendErrorMessage("你已经参与了本次挑战");
            return;
        }
        
        Participants[CurrentChallenge.Name].Add(playerName);
        args.Player.SendSuccessMessage($"你已参与挑战 [c/FFD700:{CurrentChallenge.Name}]，快去完成吧！");
        
        switch (CurrentChallenge.Type)
        {
            case ChallengeType.击杀怪物:
                args.Player.SendMessage($"目标：击杀 [c/FF5555:{CurrentChallenge.Target}] {CurrentChallenge.RequiredCount} 只", Color.Yellow);
                break;
            case ChallengeType.收集物品:
                args.Player.SendMessage($"目标：收集 [c/55FF55:{CurrentChallenge.Target}] {CurrentChallenge.RequiredCount} 个，手持物品输入 /csubmit 提交", Color.Yellow);
                break;
            case ChallengeType.生存时间:
                args.Player.SendMessage($"目标：生存 {CurrentChallenge.RequiredCount} 秒，死亡则失败", Color.Yellow);
                break;
            case ChallengeType.钓鱼:
                string fishTarget = CurrentChallenge.Target == "任何" ? "任意鱼" : CurrentChallenge.Target;
                args.Player.SendMessage($"目标：钓起 [c/55AAFF:{fishTarget}] {CurrentChallenge.RequiredCount} 条，手持鱼输入 /csubmit 提交", Color.Yellow);
                break;
        }
    }
    #endregion

    #region 击杀怪物事件
    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        if (!IsChallengeActive || CurrentChallenge == null || CurrentChallenge.Type != ChallengeType.击杀怪物)
            return;
        
        var npc = args.npc;
        if (npc == null || npc.type == 0) return;
        
        int whoAmI = npc.lastInteraction;
        if (whoAmI < 0 || whoAmI >= TShock.Players.Length) return;
        var player = TShock.Players[whoAmI];
        if (player == null || !player.RealPlayer || !player.Active) return;
        
        string playerName = player.Name;
        
        if (!Participants[CurrentChallenge.Name].Contains(playerName)) return;
        
        string npcName = Lang.GetNPCNameValue(npc.type);
        if (!npcName.Contains(CurrentChallenge.Target) && !npc.TypeName.Contains(CurrentChallenge.Target))
            return;
        
        if (!Progress.ContainsKey(playerName))
            Progress[playerName] = 0;
        
        Progress[playerName]++;
        
        if (Progress[playerName] >= CurrentChallenge.RequiredCount && !Completers[CurrentChallenge.Name].Contains(playerName))
        {
            Completers[CurrentChallenge.Name].Add(playerName);
            GiveReward(playerName, CurrentChallenge.Reward);
            player.SendSuccessMessage($"恭喜！你已完成挑战 [c/FFD700:{CurrentChallenge.Name}]！奖励已发放。");
        }
    }
    #endregion

    #region 手动提交物品指令
    private void SubmitItems(CommandArgs args)
    {
        var player = args.Player;
        if (!Config.Enabled)
        {
            player.SendErrorMessage("插件未启用");
            return;
        }
        
        if (!IsChallengeActive || CurrentChallenge == null)
        {
            player.SendErrorMessage("当前没有进行中的挑战");
            return;
        }
        
        if (CurrentChallenge.Type != ChallengeType.收集物品 && CurrentChallenge.Type != ChallengeType.钓鱼)
        {
            player.SendErrorMessage("当前挑战类型不支持提交物品");
            return;
        }
        
        string playerName = player.Name;
        if (!Participants[CurrentChallenge.Name].Contains(playerName))
        {
            player.SendErrorMessage("你还没有参与本次挑战，请先输入 /cjoin 参与");
            return;
        }
        
        if (Completers[CurrentChallenge.Name].Contains(playerName))
        {
            player.SendErrorMessage("你已经完成了本次挑战，无需再次提交");
            return;
        }
        
        int slot = player.TPlayer.selectedItem;
        var heldItem = player.TPlayer.inventory[slot];
        if (heldItem == null || heldItem.type <= 0)
        {
            player.SendErrorMessage("请手持要提交的物品");
            return;
        }
        
        string itemName = Lang.GetItemNameValue(heldItem.type);
        int originalStack = heldItem.stack;
        
        bool match;
        if (CurrentChallenge.Type == ChallengeType.钓鱼 && CurrentChallenge.Target == "任何")
        {
            match = itemName.Contains("鱼");
        }
        else
        {
            match = itemName.Contains(CurrentChallenge.Target);
        }
        
        if (!match)
        {
            player.SendErrorMessage($"你手持的物品 [c/FF5555:{itemName}] 不符合挑战目标 [c/55FF55:{CurrentChallenge.Target}]");
            return;
        }
        
        int currentProgress = Progress.ContainsKey(playerName) ? Progress[playerName] : 0;
        int remainingNeeded = CurrentChallenge.RequiredCount - currentProgress;
        if (remainingNeeded <= 0)
        {
            player.SendErrorMessage("你已经达到或超过所需数量，请勿重复提交");
            return;
        }
        
        int take = Math.Min(originalStack, remainingNeeded);
        
        // 扣除物品
        heldItem.stack -= take;
        if (heldItem.stack <= 0)
            heldItem.type = 0;
        
        // 更新进度
        if (!Progress.ContainsKey(playerName))
            Progress[playerName] = take;
        else
            Progress[playerName] += take;
        
        // 发送背包更新
        player.SendData(PacketTypes.PlayerSlot, "", player.Index, slot);
        
        player.SendSuccessMessage($"已提交 [c/55FF55:{take}] 个 {itemName}，当前进度 {Progress[playerName]}/{CurrentChallenge.RequiredCount}");
        TShock.Log.ConsoleInfo($"[AutoChallenge] 玩家 {playerName} 手动提交 {take} 个 {itemName}，当前进度 {Progress[playerName]}");
        
        // 检查是否完成
        if (Progress[playerName] >= CurrentChallenge.RequiredCount && !Completers[CurrentChallenge.Name].Contains(playerName))
        {
            Completers[CurrentChallenge.Name].Add(playerName);
            GiveReward(playerName, CurrentChallenge.Reward);
            player.SendSuccessMessage($"恭喜！你已完成挑战 [c/FFD700:{CurrentChallenge.Name}]！已扣除所需物品，奖励已发放。");
            TShock.Log.ConsoleInfo($"[AutoChallenge] 玩家 {playerName} 通过提交完成挑战");
        }
    }
    #endregion

    #region 生存类挑战检查
    private void CheckSurvivalChallenge()
    {
        foreach (var playerName in Participants[CurrentChallenge.Name].ToList())
        {
            if (Completers[CurrentChallenge.Name].Contains(playerName))
                continue;

            var player = TShock.Players.FirstOrDefault(p => p != null && p.Name == playerName && p.Active);
            if (player == null || player.Dead)
            {
                Participants[CurrentChallenge.Name].Remove(playerName);
                if (player != null)
                    player.SendErrorMessage("你在挑战中死亡，挑战失败！");
                continue;
            }

            if (!Progress.ContainsKey(playerName))
                Progress[playerName] = 0;

            Progress[playerName]++;

            if (Progress[playerName] >= CurrentChallenge.RequiredCount && !Completers[CurrentChallenge.Name].Contains(playerName))
            {
                Completers[CurrentChallenge.Name].Add(playerName);
                GiveReward(playerName, CurrentChallenge.Reward);
                player.SendSuccessMessage($"恭喜！你已生存 {CurrentChallenge.RequiredCount} 秒，完成挑战！奖励已发放。");
            }
        }
    }
    #endregion

    #region 发放奖励
    private void GiveReward(string playerName, Reward reward)
    {
        var player = TShock.Players.FirstOrDefault(p => p != null && p.Name == playerName && p.Active);
        if (player == null) return;

        switch (reward.Type)
        {
            case "物品":
                int itemId = int.TryParse(reward.Identifier, out int id) ? id : 0;
                if (itemId > 0)
                {
                    player.GiveItem(itemId, reward.Quantity);
                }
                break;

            case "Buff":
                int buffId = int.TryParse(reward.Identifier, out int bid) ? bid : 0;
                if (buffId > 0)
                {
                    player.SetBuff(buffId, 3600);
                }
                break;

            case "命令":
                TShockAPI.Commands.HandleCommand(TSPlayer.Server, reward.Identifier.Replace("{Player}", playerName));
                break;

            default:
                TShock.Log.ConsoleError($"[AutoChallenge] 未知的奖励类型: {reward.Type}");
                break;
        }
    }
    #endregion

    #region 管理指令
    private void ManageChallenge(CommandArgs args)
    {
        if (args.Parameters.Count < 1)
        {
            args.Player.SendErrorMessage("用法：/cm <start/stop/list/reload>");
            return;
        }
        
        var subCmd = args.Parameters[0].ToLower();
        
        switch (subCmd)
        {
            case "start":
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage("用法：/cm start <挑战编号>");
                    return;
                }
                
                if (int.TryParse(args.Parameters[1], out int index) && index >= 0 && index < Config.Challenges.Count)
                {
                    if (IsChallengeActive)
                    {
                        args.Player.SendErrorMessage("已有进行中的挑战，请先结束");
                        return;
                    }
                    
                    StartChallenge(Config.Challenges[index]);
                    args.Player.SendSuccessMessage($"已手动开始挑战: {Config.Challenges[index].Name}");
                }
                else
                {
                    args.Player.SendErrorMessage("无效的挑战编号");
                }
                break;
                
            case "stop":
                if (IsChallengeActive)
                {
                    EndChallenge(false);
                    args.Player.SendSuccessMessage("已强制结束当前挑战");
                }
                else
                {
                    args.Player.SendErrorMessage("没有进行中的挑战");
                }
                break;
                
            case "list":
                args.Player.SendMessage($"共有 {Config.Challenges.Count} 个挑战配置：", Color.Yellow);
                for (int i = 0; i < Config.Challenges.Count; i++)
                {
                    var c = Config.Challenges[i];
                    args.Player.SendMessage($"[{i}] {c.Name} - {c.Type}:{c.Target} x{c.RequiredCount} 奖励:{c.Reward.Type} 进度:{c.ProgressCondition ?? "无"}", Color.White);
                }
                break;
                
            case "reload":
                LoadConfig();
                args.Player.SendSuccessMessage("配置重载完成");
                break;
        }
    }
    #endregion
}