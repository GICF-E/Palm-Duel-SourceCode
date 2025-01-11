using UnityEngine;
using System.Collections;

public class LeaderBoardsManager : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("排行榜实体容器")] public Transform leaderBoardsContent;

    [Header("属性")]
    [Tooltip("排行榜实体预制件")] public GameObject entityPrefab;

    void Start()
    {
        // 等待并生成排行榜实体
        StartCoroutine(GenerateLeaderBoardsEntity());
    }

    // 生成排行榜玩家实体
    private IEnumerator GenerateLeaderBoardsEntity(){
        // 等待获取排行榜数据
        yield return new WaitUntil(() => UserData.skillLeaderboard.Count > 0);
        // 遍历创建预制体实例
        foreach(var leaderboardEntity in UserData.skillLeaderboard){
            // 生成对应的实体
            GameObject entity = Instantiate(entityPrefab, leaderBoardsContent.position, transform.rotation, leaderBoardsContent);
            LeaderBoardsEntity entityComponent = entity.GetComponent<LeaderBoardsEntity>();
            // 初始化实体
            entityComponent.Setup(leaderboardEntity.Key, leaderboardEntity.Value.Item1, leaderboardEntity.Value.Item2);
        }
    }
}