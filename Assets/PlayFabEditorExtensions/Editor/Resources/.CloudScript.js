// 封禁玩家方法
handlers.banPlayer = function (args, context) {
    // 检查客户端是否提供玩家 ID
    if (!args.PlayFabId) {
        // 返回错误信息
        return { success: false, error: "PlayFabId is required to ban a player." };
    }

    // 构造封禁请求对象
    var banRequest = {
        Bans: [
            {
                PlayFabId: args.PlayFabId,
                Reason: args.Reason || "Some Reasons",
                DurationInHours: args.DurationInHours || null
            }
        ]
    };

    try {
        // 执行封禁操作
        var result = server.BanUsers(banRequest);

        // 返回结果
        return {
            success: true,
            bannedPlayerId: args.PlayFabId,
            banDetails: result.BanData
        };
    } catch (error) {
        // 捕获异常并返回错误信息
        return { success: false, error: error };
    }
};
