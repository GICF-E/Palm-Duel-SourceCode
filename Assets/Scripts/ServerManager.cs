using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerAgent.Model;
using Mirror;

public class ServerManager : MonoBehaviour
{
    [Header("属性")]
    [Tooltip("已连接的玩家")] private List<ConnectedPlayer> currentlyConnectedPlayers = new List<ConnectedPlayer>();
    [Tooltip("目标连接玩家数")] public int targetPlayerCount = 2;

    // 记录断开的客户端及其断开时间
    private Dictionary<int, float> disconnectedClients = new Dictionary<int, float>();
    // 重连超时时间
    private float reconnectTimeout = 15f;

    private void Start()
    {
        try
        {
            // 初始化 PlayFab GSDK
            PlayFabMultiplayerAgentAPI.Start();

            // 配置 GSDK 回调
            PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
            PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
            PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
            PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

            // 准备服务器
            PlayFabMultiplayerAgentAPI.ReadyForPlayers();
            PlayFabMultiplayerAgentAPI.LogMessage("GSDK 初始化完成并准备接收玩家");

            // 启动 Mirror 服务器以处理玩家连接和游戏逻辑
            StartMirrorServer();
        }
        catch (Exception ex)
        {
            // 如果 GSDK 启动失败，记录错误信息
            Debug.LogError($"GSDK 启动失败: {ex.Message}");
            PlayFabMultiplayerAgentAPI.LogMessage($"GSDK 启动失败: {ex.Message}");
        }
    }

    private void StartMirrorServer()
    {
        // 启动 Mirror 服务器
        NetworkManager.singleton.StartServer();

        // 订阅事件
        NetworkServer.OnConnectedEvent += OnClientConnected;
        NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
        // 注册消息处理器
        NetworkServer.RegisterHandler<SyncCardMessage>(OnReceiveSyncCardMessage);
        PlayFabMultiplayerAgentAPI.LogMessage("Mirror 服务器已启动。");
    }

    // 处理同步卡牌消息
    private void OnReceiveSyncCardMessage(NetworkConnection conn, SyncCardMessage msg)
    {
        Debug.Log($"收到来自{msg.Id}客户端的同步请求: {msg.cardName}");
        // 将消息广播给所有客户端
        NetworkServer.SendToAll(msg);
    }

    private void OnClientConnected(NetworkConnection conn)
    {
        // 添加连接的玩家到列表
        currentlyConnectedPlayers.Add(new ConnectedPlayer(conn.connectionId.ToString()));
        Debug.Log($"客户端已连接: {conn.connectionId}. 当前连接数: {currentlyConnectedPlayers.Count}");

        // 通知 PlayFab 更新连接玩家信息
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(currentlyConnectedPlayers);

        // 如果已达到目标玩家数，通知切换场景
        if (currentlyConnectedPlayers.Count >= targetPlayerCount)
        {
            Debug.Log("所有客户端已连接，准备通知切换场景");
            SceneChangeMessage msg = new SceneChangeMessage { sceneName = "Game" };
            NetworkServer.SendToAll(msg);
        }
    }

    private void OnClientDisconnected(NetworkConnection conn)
    {
        int connId = conn.connectionId;
        Debug.Log($"客户端断开连接: {connId}");
        // 记录断开时间
        disconnectedClients[connId] = Time.time;

        StartCoroutine(CheckReconnect(connId));
    }

    private IEnumerator CheckReconnect(int connId)
    {
        yield return new WaitForSeconds(reconnectTimeout);

        // 检查是否仍未重新连接
        if (disconnectedClients.ContainsKey(connId) && Time.time - disconnectedClients[connId] >= reconnectTimeout)
        {
            Debug.Log($"客户端 {connId} 未能重新连接，通知其他客户端切换到 Main 场景");
            disconnectedClients.Remove(connId);

            // 从连接玩家列表中移除
            currentlyConnectedPlayers.RemoveAll(player => player.PlayerId == connId.ToString());
            PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(currentlyConnectedPlayers);

            // 如果没有玩家在线，关闭服务器
            if (currentlyConnectedPlayers.Count == 0)
            {
                Debug.Log("所有玩家已断开连接，关闭服务器...");
                ShutdownServer();
            }
            else
            {
                // 通知其他客户端返回主场景
                SceneChangeMessage msg = new SceneChangeMessage { sceneName = "Main" };
                NetworkServer.SendToAll(msg);
            }
        }
    }

    private void ShutdownServer()
    {
        // 执行清理操作并关闭服务器
        NetworkServer.Shutdown();
        Debug.Log("服务器已关闭");
        Application.Quit();
    }

    private void OnShutdown()
    {
        Debug.Log("服务器正在关闭");
        Application.Quit();
    }

    private void OnAgentError(string error)
    {
        Debug.LogError($"代理错误: {error}");
    }

    private void OnMaintenance(DateTime? maintenanceTime)
    {
        Debug.Log($"维护计划时间: {maintenanceTime}");
    }

    private void OnServerActive()
    {
        Debug.Log("服务器已从代理激活启动");
    }
}