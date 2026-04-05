using Terraria;
using TShockAPI;
using System.Text;
using Terraria.ID;
using Terraria.Utilities;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Events;
using System.Text.RegularExpressions;
using static AutoChallenge.Plugin;

namespace AutoChallenge;

internal static class Utils
{
    #region 渐变色方法
    public static string TextGradient(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var start = new Color(166, 213, 234);
        var end = new Color(245, 247, 175);

        var lines = text.Split('\n');
        var result = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
            {
                result.AppendLine();
                continue;
            }

            float ratio = (float)i / (lines.Length - 1);
            var color = Color.Lerp(start, end, ratio);
            result.AppendLine($"[c/{color.Hex3()}:{lines[i]}]");
        }

        return result.ToString();
    }

    public static string Hex3(this Color color)
    {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    #endregion

    #region 进度检测（从原 Utils 中提取）
    // 检查条件组中的所有条件是否都满足
    public static bool CheckConds(List<string> conds, Player? p = null)
    {
        foreach (var c in conds)
        {
            if (!CheckCond(c, p))
                return false;
        }
        return true;
    }

    // 检查单个条件是否满足 - 直接匹配中文
    public static bool CheckCond(string cond, Player? p = null)
    {
        switch (cond)
        {
            case "0":
            case "无":
                return true;
            case "1":
            case "克眼":
            case "克苏鲁之眼":
                return NPC.downedBoss1;
            case "2":
            case "史莱姆王":
            case "史王":
                return NPC.downedSlimeKing;
            case "3":
            case "世吞":
            case "黑长直":
            case "世界吞噬者":
            case "世界吞噬怪":
                return NPC.downedBoss2 &&
                       (IsDefeated(NPCID.EaterofWorldsHead) ||
                        IsDefeated(NPCID.EaterofWorldsBody) ||
                        IsDefeated(NPCID.EaterofWorldsTail));
            case "4":
            case "克脑":
            case "脑子":
            case "克苏鲁之脑":
                return NPC.downedBoss2 && IsDefeated(NPCID.BrainofCthulhu);
            case "5":
            case "邪恶boss2":
            case "世吞或克脑":
            case "击败世吞克脑任意一个":
                return NPC.downedBoss2;
            case "6":
            case "巨鹿":
            case "鹿角怪":
                return NPC.downedDeerclops;
            case "7":
            case "蜂王":
                return NPC.downedQueenBee;
            case "8":
            case "骷髅王前":
                return !NPC.downedBoss3;
            case "9":
            case "吴克":
            case "骷髅王":
            case "骷髅王后":
                return NPC.downedBoss3;
            case "10":
            case "肉前":
                return !Main.hardMode;
            case "11":
            case "困难模式":
            case "肉山":
            case "肉后":
            case "血肉墙":
                return Main.hardMode;
            case "12":
            case "毁灭者":
            case "铁长直":
                return NPC.downedMechBoss1;
            case "13":
            case "双子眼":
            case "双子魔眼":
                return NPC.downedMechBoss2;
            case "14":
            case "铁吴克":
            case "机械吴克":
            case "机械骷髅王":
                return NPC.downedMechBoss3;
            case "15":
            case "世纪之花":
            case "花后":
            case "世花":
                return NPC.downedPlantBoss;
            case "16":
            case "石后":
            case "石巨人":
                return NPC.downedGolemBoss;
            case "17":
            case "史后":
            case "史莱姆皇后":
                return NPC.downedQueenSlime;
            case "18":
            case "光之女皇":
            case "光女":
                return NPC.downedEmpressOfLight;
            case "19":
            case "猪鲨":
            case "猪龙鱼公爵":
                return NPC.downedFishron;
            case "20":
            case "拜月":
            case "拜月教":
            case "教徒":
            case "拜月教邪教徒":
                return NPC.downedAncientCultist;
            case "21":
            case "月总":
            case "月亮领主":
                return NPC.downedMoonlord;
            case "22":
            case "哀木":
                return NPC.downedHalloweenTree;
            case "23":
            case "南瓜王":
                return NPC.downedHalloweenKing;
            case "24":
            case "常绿尖叫怪":
                return NPC.downedChristmasTree;
            case "25":
            case "冰雪女王":
                return NPC.downedChristmasIceQueen;
            case "26":
            case "圣诞坦克":
                return NPC.downedChristmasSantank;
            case "27":
            case "火星飞碟":
                return NPC.downedMartians;
            case "28":
            case "小丑":
                return NPC.downedClown;
            case "29":
            case "日耀柱":
                return NPC.downedTowerSolar;
            case "30":
            case "星旋柱":
                return NPC.downedTowerVortex;
            case "31":
            case "星云柱":
                return NPC.downedTowerNebula;
            case "32":
            case "星尘柱":
                return NPC.downedTowerStardust;
            case "33":
            case "一王后":
            case "任意机械boss":
                return NPC.downedMechBossAny;
            case "34":
            case "三王后":
                return NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;
            case "35":
            case "一柱后":
                return NPC.downedTowerNebula || NPC.downedTowerSolar || NPC.downedTowerStardust || NPC.downedTowerVortex;
            case "36":
            case "四柱后":
                return NPC.downedTowerNebula && NPC.downedTowerSolar && NPC.downedTowerStardust && NPC.downedTowerVortex;
            case "37":
            case "哥布林入侵":
                return NPC.downedGoblins;
            case "38":
            case "海盗入侵":
                return NPC.downedPirates;
            case "39":
            case "霜月":
                return NPC.downedFrost;
            case "40":
            case "血月":
                return Main.bloodMoon;
            case "41":
            case "雨天":
                return Main.raining;
            case "42":
            case "白天":
                return Main.dayTime;
            case "43":
            case "晚上":
                return !Main.dayTime;
            case "44":
            case "大风天":
                return Main.IsItAHappyWindyDay;
            case "45":
            case "万圣节":
                return Main.halloween;
            case "46":
            case "圣诞节":
                return Main.xMas;
            case "47":
            case "派对":
                return BirthdayParty.PartyIsUp;
            case "48":
            case "旧日一":
            case "黑暗法师":
            case "撒旦一":
                return DD2Event._downedDarkMageT1;
            case "49":
            case "旧日二":
            case "巨魔":
            case "食人魔":
            case "撒旦二":
                return DD2Event._downedOgreT2;
            case "50":
            case "旧日三":
            case "贝蒂斯":
            case "双足翼龙":
            case "撒旦三":
                return DD2Event._spawnedBetsyT3;
            case "51":
            case "2020":
            case "醉酒":
            case "醉酒种子":
            case "醉酒世界":
                return Main.drunkWorld;
            case "52":
            case "2021":
            case "十周年":
            case "十周年种子":
                return Main.tenthAnniversaryWorld;
            case "53":
            case "ftw":
            case "真实世界":
            case "真实世界种子":
                return Main.getGoodWorld;
            case "54":
            case "ntb":
            case "蜜蜂世界":
            case "蜜蜂世界种子":
                return Main.notTheBeesWorld;
            case "55":
            case "dst":
            case "饥荒":
            case "永恒领域":
                return Main.dontStarveWorld;
            case "56":
            case "remix":
            case "颠倒":
            case "颠倒世界":
            case "颠倒种子":
                return Main.remixWorld;
            case "57":
            case "noTrap":
            case "陷阱种子":
            case "陷阱世界":
                return Main.noTrapsWorld;
            case "58":
            case "天顶":
            case "天顶种子":
            case "缝合种子":
            case "天顶世界":
            case "缝合世界":
                return Main.zenithWorld;
            default:
                TShock.Log.ConsoleInfo($"[AutoChallenge] 未知条件: {cond}");
                return false;
        }
    }

    // 是否解锁怪物图鉴以达到解锁物品掉落的程度（用于独立判断克脑、世吞）
    private static bool IsDefeated(int type)
    {
        var unlockState = Main.BestiaryDB.FindEntryByNPCID(type).UIInfoProvider.GetEntryUICollectionInfo().UnlockState;
        return unlockState == Terraria.GameContent.Bestiary.BestiaryEntryUnlockState.CanShowDropsWithDropRates_4;
    }
    #endregion
}