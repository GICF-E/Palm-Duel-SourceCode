using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class CardModel : MonoBehaviour
{
    [Header("边缘属性")]
    [Tooltip("卡牌边缘")] public Image cardEdge;
    [Tooltip("基本颜色")] public Color normalColor = Color.white;

    [Header("组件引用")]
    [Tooltip("卡牌名字")] public Text cardText;
    [Tooltip("Image 组件")] public Image cardImage;
    [Tooltip("卡牌伤害或回复")] public Text hintText1;
    [Tooltip("卡牌防御")] public Text hintText2;

    [Header("基本属性")]
    [Tooltip("卡牌的名字")] public string cardName = null;

    private void Start()
    {
        // 初始化颜色
        cardEdge.color = normalColor;
    }

    // 初始化卡牌方法
    public void SetupCard(Move move)
    {
        // 更新卡牌名字
        cardName = move.name;
        // 更新UI显示
        cardText.text = cardName;

        // 加载与卡牌名称相同的图片
        string imagePath = $"CardsImages/{cardName}";
        Sprite cardSprite = Resources.Load<Sprite>(imagePath);

        // 检查图片是否成功加载
        if (cardSprite != null)
        {
            // 设置卡牌图片
            cardImage.sprite = cardSprite;
            // 保持长宽比
            cardImage.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning($"找不到与卡牌名称 {cardName} 对应的图片，请检查 Resources/Cards 文件夹下是否存在 {cardName}.png");
        }

        // 获取并排序属性
        List<string> attributes = new List<string>
        {
            // 向列表添加招数的所有的属性
            "- 伤害: " + move.damage,
            "- 防御: " + move.defense,
            "- 回复: " + move.heal,
            "- 克制: " + string.Join("、", move.restrain.Select(ability => ability.key)),
            "- 消耗: " + string.Join("、", move.abilityCost.Select(ability => ability.key + " × " + ability.value)),
            "- 增加: " + string.Join("、", move.abilityIncrease.Where(ability => !ability.key.Equals("点")).Select(ability => ability.key + " × " + ability.value))
        };

        // 按照优先级排序
        attributes.Sort((a, b) =>
        {
            int GetPriority(string attr)
            {
                if (attr.StartsWith("- 伤害")) return move.damage == 0 ? 7 : 1;
                if (attr.StartsWith("- 防御")) return move.defense == 0 ? 7 : 2;
                if (attr.StartsWith("- 回复")) return move.heal == 0 ? 7 : 3;
                if (attr.StartsWith("- 克制")) return move.restrain.Length == 0 ? 7 : 4;
                if (attr.StartsWith("- 消耗")) return move.abilityCost.Length == 0 ? 7 : 5;
                if (attr.StartsWith("- 增加")) return move.abilityIncrease.Length == 0 ? 7 : 6;
                return 7;
            }
            return GetPriority(a).CompareTo(GetPriority(b));
        });

        // 更新UI提示
        hintText1.text = attributes.Count > 0 ? attributes[0] : "";
        hintText2.text = attributes.Count > 1 ? attributes[1] : "";
    }
}