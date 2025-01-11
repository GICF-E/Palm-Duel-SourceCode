using UnityEngine;
using Mirror;

public class SyncManager : MonoBehaviour
{
    [Header("属性")]
    [Tooltip("全局唯一标识符")] private string GUID;
    [Tooltip("当前远程玩家选择的卡牌名称")] private string remotePlayerCurrentSelectCardName = null;
    [Tooltip("Referee")] public Referee referee;
    [Tooltip("本地玩家")] public Player localPlayer;
    [Tooltip("远程玩家")] public Player remotePlayer;

    private void Awake()
    {
        // 注册消息处理器
        NetworkClient.RegisterHandler<SyncCardMessage>(OnSyncCardMessageHandler);
        // 生成GUID
        GUID = System.Guid.NewGuid().ToString();
    }

    // 向服务器发送卡牌选择
    public void CardSelected(string cardName)
    {
        Debug.Log("发送卡牌消息: " + cardName);

        // 创建 SyncCardMessage
        SyncCardMessage msg = new SyncCardMessage
        {
            Id = GUID,
            cardName = cardName
        };

        // 发送消息到服务器
        NetworkClient.Send(msg);

        // 禁用选择
        localPlayer.SetCardSelectionEnabled(false);
    }

    // 启动同步携程的回调
    private void OnSyncCardMessageHandler(SyncCardMessage msg)
    {
        // 检查消息ID，确保不是自己发送的消息
        if (msg.Id != GUID && msg.cardName != "CheckBack")
        {
            // 更新选择的卡牌
            remotePlayerCurrentSelectCardName = msg.cardName;
            // 更新选择指示器
            remotePlayer.selectionIndicator.color = remotePlayer.selectionIndicator.color == Color.green ? Color.grey : Color.green;
            // 发送返回确认信息
            SyncCardMessage confirmMsg = new SyncCardMessage
            {
                Id = GUID,
                cardName = "CheckBack"
            };
            NetworkClient.Send(confirmMsg);
        }
        else if (msg.Id != GUID && msg.cardName == "CheckBack")
        {
            // 收到自己的确认信息，启用选择
            localPlayer.SetCardSelectionEnabled(true);
        }
    }

    private void Update()
    {
        // 更新本地玩家的选择指示器
        localPlayer.selectionIndicator.color = localPlayer.selectCard == null ? Color.grey : Color.green;
        // 检查本地玩家是否选择卡牌
        if (!referee.isProcessingCard && localPlayer.selectCard != null && remotePlayerCurrentSelectCardName != null)
        {
            // 选择卡牌
            SelectRemotePlayerCard(remotePlayerCurrentSelectCardName);
        }
    }

    // 选择远程玩家的卡牌
    private void SelectRemotePlayerCard(string cardName)
    {
        Debug.Log($"收到来自其他客户端的卡牌选择消息: {cardName}");
        // 在远程玩家的卡牌列表中查找选择的卡牌
        Card selectedCard = remotePlayer?.cards.Find(c => c.cardName == cardName);
        // 切换卡牌状态为选中状态
        selectedCard.ChangeCardState();
        // 清空当前选择的卡牌名称
        remotePlayerCurrentSelectCardName = null;
    }
}