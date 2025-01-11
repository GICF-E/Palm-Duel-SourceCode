using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Newtonsoft.Json;

public class UserDataManager : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("匹配器")] public Matchmaker matchmaker;

    [Header("属性")]
    [Tooltip("排行榜名称")] public string leaderboardName = "SkillLeaderboards";
    [Tooltip("排行榜最大结果数")] public int maxResultsCount = 20;

    private void Awake()
    {
        // 获取并存储用户数据
        GetAndSaveUserData();
        GetLeaderboard();
    }

    // 获取并存储用户数据
    private void GetAndSaveUserData()
    {
        // 获取账户信息
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            // 写入本地玩家的账户信息
            UserData.playerEmail = result.AccountInfo.PrivateInfo.Email;
            UserData.playerPlayFabId = result.AccountInfo.PlayFabId;
            UserData.playerDisplayName = result.AccountInfo.TitleInfo.DisplayName;
        }, matchmaker.OnError);

        // 获取用户数据，包括经验值、总对战场次和胜利场次
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), userDataResult =>
        {
            // 检查并获取用户数据
            if (userDataResult.Data != null)
            {
                // 获取经验值
                if (userDataResult.Data.ContainsKey("Skill"))
                {
                    UserData.UpdatePlayerSkill(int.Parse(userDataResult.Data["Skill"].Value));
                }
                else
                {
                    // 如果经验值不存在，则初始化
                    InitializeUserData("Skill", "20");
                    UserData.UpdatePlayerSkill(20);
                }
                // 获取总对战场次
                if (userDataResult.Data.ContainsKey("AttendedGames"))
                {
                    UserData.playerAttendedGames = int.Parse(userDataResult.Data["AttendedGames"].Value);
                }
                else
                {
                    // 如果总对战场次不存在，则初始化
                    InitializeUserData("AttendedGames", "0");
                    UserData.playerAttendedGames = 0;
                }

                // 获取胜利场次
                if (userDataResult.Data.ContainsKey("WinGames"))
                {
                    UserData.playerWinGames = int.Parse(userDataResult.Data["WinGames"].Value);
                }
                else
                {
                    // 如果胜利场次不存在，则初始化
                    InitializeUserData("WinGames", "0");
                    UserData.playerWinGames = 0;
                }

                // 获取历史记录
                if (userDataResult.Data.ContainsKey("HistoryRecords"))
                {
                    // 反序列化 JSON 为 List
                    UserData.historyRecords = JsonConvert.DeserializeObject<List<(string, string)>>(userDataResult.Data["HistoryRecords"].Value);
                }
                else
                {
                    // 如果历史记录不存在，则初始化
                    InitializeUserData("HistoryRecords", JsonConvert.SerializeObject(new List<(string, string)>()));
                    UserData.historyRecords = new List<(string, string)>();
                }
            }
        }, matchmaker.OnError);
    }

    // 初始化用户数据
    private void InitializeUserData(string key, string value)
    {
        // 构造更新用户数据请求
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {key, value}
            }
        };

        // 发送更新用户数据请求
        PlayFabClientAPI.UpdateUserData(request, result => Debug.Log($"{key} 数据初始化成功为 {value}"), matchmaker.OnError);
    }

    // 调用此方法以获取排行榜
    public void GetLeaderboard()
    {
        // 构造获取排行榜请求
        var request = new GetLeaderboardRequest
        {
            StatisticName = leaderboardName,
            StartPosition = 0,
            MaxResultsCount = maxResultsCount
        };

        // 发送获取排行榜请求
        PlayFabClientAPI.GetLeaderboard(request, OnGetLeaderboardSuccess, matchmaker.OnError);
    }

    // 获取排行成功
    private void OnGetLeaderboardSuccess(GetLeaderboardResult result)
    {
        // 获取排行榜数据
        foreach (var leaderboardEntity in result.Leaderboard)
        {
            // 使用ValueTuple来存储排名和分数
            var displayNameAndScore = (Rank: leaderboardEntity.DisplayName, Score: leaderboardEntity.StatValue);

            // 存储数据
            UserData.skillLeaderboard.Add(leaderboardEntity.Position + 1, displayNameAndScore);
        }
    }
}