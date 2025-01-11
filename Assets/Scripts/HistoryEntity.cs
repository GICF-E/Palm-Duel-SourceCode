using UnityEngine;
using UnityEngine.UI;

public class HistoryEntity : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("名次文本")] public Text rankingText;
    [Tooltip("显示名称文本")] public Text opponentDisplayNameText;
    [Tooltip("经验值文本")] public Text skillText;

    // 初始化方法
    public void Setup(int ranking, string opponentDisplayName, string skill)
    {
        // 初始化文本
        rankingText.text = ranking.ToString();
        opponentDisplayNameText.text = opponentDisplayName;
        skillText.text = skill.ToString();
    }
}