using Mirror;

// 用于同步卡牌选择的消息
public struct SyncCardMessage : NetworkMessage
{
    public string Id;
    public string cardName;
}

// 用于场景切换的消息
public struct SceneChangeMessage : NetworkMessage
{
    public string sceneName;
}