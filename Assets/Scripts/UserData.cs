using System.Collections.Generic;
using UnityEngine;

public static class UserData
{
    // 本地玩家数据
    public static string playerEmail = null;
    public static string playerPlayFabId = null;
    public static string playerDisplayName = null;
    public static string playerSkill; // 加密存储
    public static int playerAttendedGames = -1;
    public static int playerWinGames = -1;

    // 远程玩家数据
    public static string remotePlayerDisplayName = null;
    public static string remotePlayerSkill; // 加密存储

    // 经验值排行榜
    public static SortedDictionary<int, (string, int)> skillLeaderboard = new SortedDictionary<int, (string, int)>();

    // 玩家对战历史记录
    public static List<(string, string)> historyRecords = new List<(string, string)>();

    // 重置所有玩家数据
    public static void ResetData()
    {
        // 重置本地玩家数据
        playerEmail = null;
        playerPlayFabId = null;
        playerDisplayName = null;
        playerSkill = null;
        playerAttendedGames = -1;
        playerWinGames = -1;

        // 重置远程玩家数据
        remotePlayerDisplayName = null;
        remotePlayerSkill = null;

        // 清空经验值排行榜
        skillLeaderboard.Clear();

        // 清空玩家对战历史记录
        historyRecords.Clear();
    }

    // 更新本地玩家的Skill数据
    public static void UpdatePlayerSkill(int skill)
    {
        // 加密 Skill 数据
        playerSkill = AESManager.Encrypt(skill.ToString());
    }

    // 获取解密后的本地玩家Skill数据
    public static int GetPlayerSkill()
    {
        if (string.IsNullOrEmpty(playerSkill)) Debug.LogWarning("Player skill 数据未设置");
        // 解密 Skill 数据并返回
        return int.Parse(AESManager.Decrypt(playerSkill));
    }

    // 更新远程玩家的Skill数据
    public static void UpdateRemotePlayerSkill(int skill)
    {
        // 加密 Skill 数据
        remotePlayerSkill = AESManager.Encrypt(skill.ToString());
    }

    // 获取解密后的远程玩家Skill数据
    public static int GetRemotePlayerSkill()
    {
        if (string.IsNullOrEmpty(remotePlayerSkill)) Debug.LogWarning("Remote player skill 数据未设置。");
        // 解密 Skill 数据并返回
        return int.Parse(AESManager.Decrypt(remotePlayerSkill));
    }
}