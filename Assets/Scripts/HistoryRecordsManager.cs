using UnityEngine;
using System.Collections;

public class HistoryRecordsManager : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("历史记录实体容器")] public Transform historyRecordsContent;

    [Header("属性")]
    [Tooltip("历史记录实体预制件")] public GameObject entityPrefab;

    void Start()
    {
        // 等待并生成历史记录实体
        StartCoroutine(GenerateLeaderBoardsEntity());
    }

    // 生成历史记录玩家实体
    private IEnumerator GenerateLeaderBoardsEntity()
    {
        // 计数器
        int counter = 0;

        // 等待获取历史记录数据
        yield return new WaitUntil(() => UserData.historyRecords.Count > 0);

        // 遍历创建预制体实例
        foreach (var historyRecordEntity in UserData.historyRecords)
        {
            // 更新计数器
            counter++;
            // 生成对应的实体
            GameObject entity = Instantiate(entityPrefab, historyRecordsContent.position, transform.rotation, historyRecordsContent);
            HistoryEntity entityComponent = entity.GetComponent<HistoryEntity>();
            // 初始化实体
            entityComponent.Setup(counter, historyRecordEntity.Item1, historyRecordEntity.Item2);
        }
    }
}