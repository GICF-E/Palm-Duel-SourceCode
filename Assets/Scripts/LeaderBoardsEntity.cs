using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardsEntity : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("名次文本")] public Text rankingText;
    [Tooltip("显示名称文本")] public Text displayNameText;
    [Tooltip("经验值文本")] public Text skillText;

    [Header("属性")]
    [Tooltip("金牌颜色")] public Color gordColor;
    [Tooltip("银牌颜色")] public Color silverColor;
    [Tooltip("铜牌颜色")] public Color copperyColor;
    [Tooltip("其他排名颜色")] public Color normalColor;

    // 初始化方法
    public void Setup(int ranking, string displayName, int skill)
    {
        // 初始化文本
        rankingText.text = ranking.ToString();
        displayNameText.text = displayName;
        skillText.text = skill.ToString();
        
        // 根据名次更改名次文本颜色
        if (ranking == 1) rankingText.color = gordColor;
        else if (ranking == 2) rankingText.color = silverColor;
        else if (ranking == 3) rankingText.color = copperyColor;
        else rankingText.color =normalColor;
    }
}