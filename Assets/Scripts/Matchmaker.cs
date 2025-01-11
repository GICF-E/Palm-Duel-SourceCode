using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using kcp2k;

public class Matchmaker : MonoBehaviour
{
    [Header("属性")]
    [Tooltip("匹配队列名称")] public string queueName = "MainQueue";
    [Tooltip("是否在匹配")] private bool isMatchmaking = false;
    [Tooltip("当前匹配票证")] private string currentTicketId;
    [Tooltip("区域选择延迟")] private Dictionary<string, int> regionLatencies = new Dictionary<string, int>();

    [Header("引用")]
    [Tooltip("按钮文本")] public Text buttonText;
    [Tooltip("匹配按钮颜色")] public Image buttonImage;
    [Tooltip("匹配按钮正常颜色")] public Color normalColor;
    [Tooltip("匹配按钮选择颜色")] public Color selectedColor;

    private void Start()
    {
        // 初始化匹配按钮文本
        buttonText.text = "快速匹配";
        // 初始化匹配按钮颜色
        buttonImage.color = normalColor;
        // 初始化变量
        isMatchmaking = false;
        // 注册消息处理器
        NetworkClient.RegisterHandler<SceneChangeMessage>(OnSceneChangeMessage);
        // 开始测量延迟
        PlayFabMultiplayerAPI.ListQosServersForTitle(new ListQosServersForTitleRequest(), MeasureLatencies, OnError);
    }

    // 按下匹配按钮
    public void OnClickMatchButton()
    {
        // 切换匹配状态和匹配按钮状态
        isMatchmaking = !isMatchmaking;
        buttonText.text = isMatchmaking ? "匹配中..." : "快速匹配";
        buttonImage.color = isMatchmaking ? selectedColor : normalColor;

        if (isMatchmaking)
        {
            // 创建匹配票证
            StartCoroutine(StartMatchmaking());
        }
        else
        {
            // 取消匹配票证
            CancelMatchmakingTicket();
        }
    }

    // 创建匹配票证
    private IEnumerator StartMatchmaking()
    {
        // 等待数据加载完成
        yield return new WaitUntil(() => regionLatencies.Count != 0 && UserData.playerDisplayName != null && UserData.playerSkill != null);

        // 创建匹配票据请求
        var request = new CreateMatchmakingTicketRequest
        {
            QueueName = queueName,
            Creator = new MatchmakingPlayer
            {
                Entity = new EntityKey
                {
                    Id = PlayFabSettings.staticPlayer.EntityId,
                    Type = PlayFabSettings.staticPlayer.EntityType
                },
                Attributes = new MatchmakingPlayerAttributes
                {
                    DataObject = new
                    {
                        Latencies = regionLatencies.Select(region => new { region = region.Key, latency = region.Value }).ToArray(),
                        DisplayName = UserData.playerDisplayName,
                        Skill = UserData.GetPlayerSkill()
                    }
                }
            },
            // 设置匹配超时时间
            GiveUpAfterSeconds = 120
        };

        // 发起创建匹配票据请求
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(request, OnMatchmakingTicketCreated, OnError);
    }

    // 匹配票据创建成功的回调方法
    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result)
    {
        Debug.Log("创建匹配票证成功");
        currentTicketId = result.TicketId;
        InvokeRepeating(nameof(CheckMatchmakingStatus), 3f, 5f);
    }

    // 轮询匹配状态的方法
    private void CheckMatchmakingStatus()
    {
        if (string.IsNullOrEmpty(currentTicketId))
        {
            return;
        }

        Debug.Log("检查匹配状态");

        // 创建获取匹配票据状态的请求
        var request = new GetMatchmakingTicketRequest
        {
            TicketId = currentTicketId,
            QueueName = queueName
        };

        // 发起获取匹配票据状态的请求
        PlayFabMultiplayerAPI.GetMatchmakingTicket(request, OnMatchmakingStatus, OnError);
    }

    // 获取匹配状态的回调方法
    private void OnMatchmakingStatus(GetMatchmakingTicketResult result)
    {
        // 如果匹配成功
        if (result.Status == "Matched")
        {
            Debug.Log("匹配成功");
            // 更新按钮文本
            buttonText.text = "连接中...";
            // 停止轮询
            CancelInvoke(nameof(CheckMatchmakingStatus));
            // 处理匹配成功后的逻辑
            GetMatch(result.MatchId);
        }
        else if (result.Status == "WaitingForPlayers")
        {
            Debug.Log("等待更多玩家加入匹配");
        }
        else if (result.Status == "WaitingForMatch")
        {
            Debug.Log("正在寻找匹配");
        }
        else
        {
            Debug.LogWarning(result.Status);
        }
    }

    // 获取匹配信息
    private void GetMatch(string matchId)
    {
        // 创建获取多人游戏服务器信息的请求
        var request = new GetMatchRequest
        {
            MatchId = matchId,
            QueueName = queueName,
            ReturnMemberAttributes = true
        };

        Debug.Log("Getting Match");

        // 发起获取请求
        PlayFabMultiplayerAPI.GetMatch(request, OnGetMatch, OnError);
    }

    // 获取匹配信息的回调
    private void OnGetMatch(GetMatchResult result)
    {
        // 获取服务器信息
        var serverDetails = result.ServerDetails;
        if (serverDetails != null)
        {
            // 获取服务器IP地址和端口
            string ipAddress = serverDetails.IPV4Address;
            int port = serverDetails.Ports[0].Num;

            // 获取匹配的所有玩家信息
            foreach (var player in result.Members)
            {
                // 获取玩家数据并转换为字典
                var jsonObject = player.Attributes.DataObject as PlayFab.Json.JsonObject;
                var dataObject = ConvertJsonObjectToDictionary(jsonObject);

                if (dataObject != null)
                {
                    // 存储玩家数据
                    string playerDisplayName = dataObject["DisplayName"].ToString();
                    int playerSkill = int.Parse(dataObject["Skill"].ToString());

                    // 判断是否是本地玩家
                    if (player.Entity.Id == PlayFabSettings.staticPlayer.EntityId)
                    {
                        // 存储本地玩家数据
                        UserData.playerDisplayName = playerDisplayName;
                        UserData.UpdatePlayerSkill(playerSkill);
                    }
                    else
                    {
                        // 存储远程玩家数据
                        UserData.remotePlayerDisplayName = playerDisplayName;
                        UserData.UpdateRemotePlayerSkill(playerSkill);
                    }
                }
                else
                {
                    Debug.LogError("DataObject 无法转换为字典类型");
                    Debug.Log($"DataObject 类型: {player.Attributes.DataObject.GetType()}");
                }
            }

            // 连接到游戏服务器
            ConnectToServer(ipAddress, port);
        }
    }

    // 连接到游戏服务器
    private void ConnectToServer(string ipAddress, int port)
    {
        Debug.Log($"Connecting to {ipAddress}:{port}");

        // 重置状态
        isMatchmaking = false;
        // 清除当前匹配票据ID
        currentTicketId = null;

        // 假设你使用 Mirror 网络库
        NetworkManager.singleton.networkAddress = ipAddress;
        NetworkManager.singleton.GetComponent<KcpTransport>().port = (ushort)port;
        NetworkManager.singleton.StartClient();
    }

    // 取消匹配票证
    private void CancelMatchmakingTicket()
    {
        if (string.IsNullOrEmpty(currentTicketId))
        {
            return;
        }

        var request = new CancelMatchmakingTicketRequest
        {
            TicketId = currentTicketId,
            QueueName = queueName
        };

        PlayFabMultiplayerAPI.CancelMatchmakingTicket(request, OnCancelMatchmakingTicket, OnError);
    }

    private void OnCancelMatchmakingTicket(CancelMatchmakingTicketResult result)
    {
        // 重置状态
        buttonText.text = "快速匹配";
        buttonImage.color = normalColor;
        isMatchmaking = false;
        currentTicketId = null;
    }

    // 处理服务器要求的场景切换
    private void OnSceneChangeMessage(SceneChangeMessage msg)
    {
        Debug.Log($"收到场景切换消息，切换到场景: {msg.sceneName}");
        // 执行场景切换
        SceneManager.LoadScene(msg.sceneName);
    }

    // 将JsonObject转换为Dictionary
    private Dictionary<string, object> ConvertJsonObjectToDictionary(PlayFab.Json.JsonObject jsonObject)
    {
        // 定义返回字典
        Dictionary<string, object> dict = new Dictionary<string, object>();

        // 转译字典
        foreach (var kvp in jsonObject)
        {
            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }

    // 开始与Agent对战的对外接口
    public void StartAgent()
    {
        StartCoroutine(StartGameWithAgent());
    }

    // 开始与Agent对战
    private IEnumerator StartGameWithAgent()
    {
        yield return new WaitUntil(() => UserData.playerSkill != null && UserData.playerAttendedGames != -1 && UserData.playerWinGames != -1);
        // 切换场景
        SceneManager.LoadScene("Agent");
    }

    // Ping 延迟测速方法
    private void MeasureLatencies(ListQosServersForTitleResponse response)
    {
        foreach (var server in response.QosServers)
        {
            // 启动对每个服务器的延迟测量
            StartCoroutine(MeasureLatency(server.Region, server.ServerUrl));
        }
    }

    // Ping 延迟测速
    private IEnumerator MeasureLatency(string regionName, string serverUrl)
    {
        yield return null;

        int attempts = 5; // 测量次数
        int totalLatency = 0; // 累计延迟时间

        // 将服务器地址解析为 IP 地址
        string ipAddress = System.Net.Dns.GetHostAddresses(serverUrl)[0].ToString();

        for (int i = 0; i < attempts; i++)
        {
            // 使用 Unity 的 Ping 类创建延迟测量请求
            Ping ping = new Ping(ipAddress);
        
            // 等待 Ping 请求完成
            yield return new WaitUntil(() => ping.isDone);

            // 累加延迟时间
            totalLatency += ping.time;
        }

        // 计算平均延迟
        int averageLatency = totalLatency / attempts;

        // 如果测量失败（即所有 Ping 请求都失败），设置为最大值
        if (averageLatency == -1)
        {
            averageLatency = int.MaxValue;
        }

        // 将测量的平均延迟存储到区域延迟字典中
        regionLatencies[regionName] = averageLatency;
        Debug.Log($"地区: {regionName}, IP: {ipAddress}, 平均延迟: {averageLatency} ms");
    }

    // 处理错误的回调
    public void OnError(PlayFabError error)
    {
        // 定义错误信息
        string errorMessage;

        // 根据不同的错误代码生成错误信息
        switch (error.Error)
        {
            case PlayFabErrorCode.ConnectionError:
                errorMessage = "网络连接错误";
                break;
            default:
                errorMessage = "发生错误，请稍后再试";
                break;
        }

        // 取消匹配票证
        if (!string.IsNullOrEmpty(currentTicketId)) CancelMatchmakingTicket();

        // 设置错误信息到按钮文本
        buttonText.text = errorMessage;

        // 重置状态
        buttonImage.color = normalColor;
        isMatchmaking = false;

        // 清除当前匹配票据ID
        currentTicketId = null;
        Debug.LogWarning(error.GenerateErrorReport());
    }
}