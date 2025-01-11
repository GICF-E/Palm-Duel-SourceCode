using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Player : MonoBehaviour
{
    [Serializable]
    public struct PlayerStats
    {
        [Tooltip("玩家血量")] public float hp;
        [Tooltip("玩家被冻结的回合数")] public float freezeTime;
        [Tooltip("无敌时间")] public float invulnerableTime;
        [Tooltip("每个类的能量点")] public Dictionary<string, int> abilityPoints;
        [Tooltip("将y伤平均分配给x个回合")] public Tuple<int, int> damageDistribution;
        [Tooltip("反伤的回合数")] public int damageReductionTime;
        [Tooltip("下回合伤害乘区")] public float nextDamageMultiplier;
        [Tooltip("冷冻后要结算的伤害")] public float freezingEndDamage;
        [Tooltip("当前卡牌的剩余防御力")] public float currentDefense;
        [Tooltip("上一次是否出的是闪")] public bool isLastCardDodge;
    }

    [Header("属性")]
    [Tooltip("是否是本地玩家")] public bool isLocalPlayer;
    [Tooltip("玩家统计信息")] public PlayerStats stats;
    [Tooltip("卡牌代码列表")] public List<Card> cards;
    [HideInInspector][Tooltip("当前选择的卡牌")] public Card selectCard;
    [HideInInspector][Tooltip("招数数据")] public MovesList movesList;

    [Header("引用")]
    [Tooltip("同步管理器")] public SyncManager syncManager;
    [Tooltip("卡牌预制件")] public GameObject cardPrefab;
    [Tooltip("卡牌容器")] public Transform content;
    [Tooltip("经验值文本")] public Text skillText;
    [Tooltip("用户名文本")] public Text displayNameText;
    [Tooltip("能量点提示文本")] public Text abilityPointsText;
    [Tooltip("能量点总和提示文本")] public Text totalAbilityPointsText;
    [Tooltip("血量提示文本")] public Text hpText;
    [Tooltip("冷冻提示组建")] public GameObject freezeComponent;
    [Tooltip("冻结提示文本")] public Text freezeText;
    [Tooltip("卡牌选择指示器")] public Image selectionIndicator;

    [Header("置中动画")]
    [Tooltip("滚动视图")] public ScrollRect scrollRect;
    [Tooltip("滚动动画持续时间")] public float scrollDuration = 0.3f;
    [Tooltip("滚动动画的曲线")] public AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        // 初始化玩家信息
        stats.hp = 1;
        stats.invulnerableTime = 0;
        stats.freezeTime = 0;
        stats.damageReductionTime = 0;
        stats.nextDamageMultiplier = 1;
        stats.freezingEndDamage = 0;
        stats.currentDefense = 0;
        stats.isLastCardDodge = false;

        // 初始化字典
        stats.abilityPoints = new Dictionary<string, int>();
        var abilities = new string[] { "点Chee", "卧倒", "招募令", "石头", "钢板", "加点", "扣点", "diamond", "点", "拳头", "鱼", "组合", "总和" };
        foreach (var ability in abilities)
        {
            stats.abilityPoints[ability] = 0;
        }
        stats.damageDistribution = new Tuple<int, int>(0, 0);

        // 初始化提示文本
        skillText.text = isLocalPlayer ? UserData.GetPlayerSkill().ToString() : UserData.GetRemotePlayerSkill().ToString();
        displayNameText.text = isLocalPlayer ? UserData.playerDisplayName : UserData.remotePlayerDisplayName;
        abilityPointsText.text = "";
        hpText.text = stats.hp.ToString();
        freezeText.text = stats.freezeTime.ToString();

        // 初始化组件
        freezeComponent.SetActive(false);
        selectionIndicator.color = Color.grey;

        // 加载招数数据
        LoadMovesData();
        ReGenerateCards();

        // 更新UI
        UpdateUI();
    }

    // 加载招数数据
    private void LoadMovesData()
    {
        TextAsset movesJson = Resources.Load<TextAsset>("MovesData");
        if (movesJson != null)
        {
            movesList = JsonUtility.FromJson<MovesList>(movesJson.text);
        }
        else
        {
            Debug.LogError("Failed to load moves data.");
        }
    }

    // 启用或禁用卡牌选择
    public void SetCardSelectionEnabled(bool enabled)
    {
        foreach (Card card in cards)
        {
            card.button.interactable = enabled;
        }
    }

    // 自动生成卡牌
    public void ReGenerateCards()
    {
        // 清除所有的卡牌
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        // 清空列表
        cards = new List<Card>();

        // 遍历所有招数
        foreach (var move in movesList.moves)
        {
            // 玩家是否可以负担这个卡牌的消耗
            bool canAffordMove = true;

            // 检查每种能力类型和消耗
            foreach (var cost in move.abilityCost)
            {
                if (stats.abilityPoints[cost.key] < cost.value)
                {
                    // 如果任一能力不足，则不能执行此招数
                    canAffordMove = false;
                    break;
                }
            }

            // 如果玩家能负担这个招数的消耗且上次没有出闪
            if (canAffordMove && !(move.name == "闪" && stats.isLastCardDodge))
            {
                // 生成对应的卡牌
                GameObject card = Instantiate(cardPrefab, content.position, transform.rotation, content);
                Card cardComponent = card.GetComponent<Card>();
                if (cardComponent != null)
                {
                    // 通过SetupCard()方法来初始化卡牌
                    cardComponent.SetupCard(move);
                    cards.Add(cardComponent);
                }
            }
        }
    }

    // 检查卡牌选中状态
    public void CheckSelectCard(Card selectedCard)
    {
        // 遍历检查所有卡牌
        foreach (Card card in cards)
        {
            // 如果卡牌是选中状态并且不是当前选中的卡牌
            if (card.isSelect && card != selectedCard)
            {
                // 将卡牌切换成未选中状态
                card.ChangeCardState();
            }
            // 如果卡牌不是选中的状态并且是当前选中的卡牌
            if (!card.isSelect && card == selectCard)
            {
                // 将卡牌切换成选中状态
                card.ChangeCardState();
            }
        }
    }

    // 将卡牌居中
    public IEnumerator CenterCard(Card card)
    {
        // 强制立即更新画布，确保所有UI元素的位置和尺寸是最新的
        Canvas.ForceUpdateCanvases();

        // 获取卡牌在cards列表中的索引
        int cardIndex = cards.IndexOf(card);
        // 如果卡牌不在列表中不执行任何操作
        if (cardIndex == -1) yield break;

        // 获取卡牌的宽度
        float cardWidth = card.GetComponent<RectTransform>().rect.width;
        // 获取GridLayoutGroup的水平间距
        float spacing = content.GetComponent<GridLayoutGroup>().spacing.x;
        // 获取GridLayoutGroup的左侧填充
        float padding = content.GetComponent<GridLayoutGroup>().padding.left;

        // 计算目标卡牌的中心位置X坐标
        float targetX = cardIndex * (cardWidth + spacing) + padding + cardWidth / 2;
        // 获取content的总宽度
        float contentWidth = scrollRect.content.rect.width;
        // 获取视窗的宽度
        float viewportWidth = scrollRect.viewport.rect.width;

        // 计算标准化位置
        float normalizedPosition = (targetX - viewportWidth / 2) / (contentWidth - viewportWidth);
        // 确保值在0到1之间
        normalizedPosition = Mathf.Clamp01(normalizedPosition);

        // 获取当前滚动位置作为动画的起始位置
        float startNormalizedPosition = scrollRect.horizontalNormalizedPosition;
        float timer = 0f;

        // 动画过程
        while (timer < scrollDuration)
        {
            // 根据设定的动画曲线计算当前进度
            float progress = scrollCurve.Evaluate(timer / scrollDuration);
            // 线性插值计算当前滚动位置
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startNormalizedPosition, normalizedPosition, progress);
            // 更新计时器
            timer += Time.deltaTime;
            yield return null;
        }

        // 确定精确目标位置
        scrollRect.horizontalNormalizedPosition = normalizedPosition;
    }

    // 更新提示文本
    public void UpdateUI()
    {
        // 更新血量提示文本
        hpText.text = stats.hp.ToString();

        // 更新冷冻提示文本
        freezeText.text = stats.freezeTime.ToString();
        // 按需显示冷冻组件
        freezeComponent.SetActive(stats.freezeTime > 0);

        // 重置更新能力点提示文本
        abilityPointsText.text = "";
        // 更新能力点总和
        totalAbilityPointsText.text = "总和 × " + stats.abilityPoints["总和"];

        // 遍历能力点以更新提示点文本
        foreach (var ability in stats.abilityPoints)
        {
            // 如果有该类型的能力点并且不是“总和”
            if (ability.Value > 0 && ability.Key != "总和" && ability.Key != "点")
            {
                // 更新提示文本
                abilityPointsText.text += ability.Key + " × " + ability.Value + "  ";
            }
        }
    }

    private void Update()
    {
        // 检查是否有卡牌被选中
        CheckSelectCard(selectCard);
    }
}