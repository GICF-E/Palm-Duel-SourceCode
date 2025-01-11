using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class PlayerTrain : Agent
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
    [Tooltip("是否是本地玩家")] public bool isPlayer;
    [Tooltip("是否处于录制状体")] public bool isRecording;
    [HideInInspector][Tooltip("是否已经初始化")] public bool isInitialized = false;
    [Tooltip("我方玩家统计信息")] public PlayerStats stats;
    [Tooltip("对方玩家统计信息")] public PlayerTrain opponentPlayer;
    [Tooltip("卡牌代码列表")] public List<CardForTrain> cards;
    [HideInInspector][Tooltip("当前选择的卡牌")] public CardForTrain selectCard;
    [Tooltip("卡牌存在列表")] public List<bool> cardSelectability = new List<bool>();
    [HideInInspector][Tooltip("上一次玩家状态")] public PlayerStats previousStats = new PlayerStats();
    [HideInInspector][Tooltip("招数数据")] public MovesList movesList;

    [Header("引用")]
    [Tooltip("行为参数")] public BehaviorParameters behaviorParameters;
    [Tooltip("卡牌预制件")] public GameObject cardPrefab;
    [Tooltip("卡牌容器")] public Transform content;
    [Tooltip("能量点提示文本")] public Text abilityPointsText;
    [Tooltip("能量点总和提示文本")] public Text totalAbilityPointsText;
    [Tooltip("血量提示文本")] public Text hpText;
    [Tooltip("冷冻提示组建")] public GameObject freezeComponent;
    [Tooltip("冻结提示文本")] public Text freezeText;

    [Header("置中动画")]
    [Tooltip("滚动视图")] public ScrollRect scrollRect;
    [Tooltip("滚动动画持续时间")] public float scrollDuration = 0.3f;
    [Tooltip("滚动动画的曲线")] public AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public override void OnEpisodeBegin()
    {
        // 重置标记
        isInitialized = false;

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
        cardSelectability = new List<bool>();
        stats.abilityPoints = new Dictionary<string, int>();
        var abilities = new string[] { "点Chee", "卧倒", "招募令", "石头", "钢板", "加点", "扣点", "diamond", "点", "拳头", "鱼", "组合", "总和" };
        foreach (var ability in abilities)
        {
            stats.abilityPoints[ability] = 0;
        }
        stats.damageDistribution = new Tuple<int, int>(0, 0);

        // 初始化提示文本
        abilityPointsText.text = "";
        hpText.text = stats.hp.ToString();
        freezeText.text = stats.freezeTime.ToString();

        // 初始化组件
        freezeComponent.SetActive(false);
        behaviorParameters = GetComponent<BehaviorParameters>();

        // 初始化上一次状态
        previousStats = stats;

        // 加载招数数据
        LoadMovesData();
        ReGenerateCards();

        // 更新UI
        UpdateUI();

        // 初始化完成
        isInitialized = true;
    }

    // 模型输出
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 获取第一个离散动作输出 
        int selectedCardIndex = actions.DiscreteActions[0];

        // 使用模型的输出执行相应的游戏逻辑
        if (!isRecording)
        {
            if (selectedCardIndex >= 0 && selectedCardIndex <= cardSelectability.Count && cardSelectability[selectedCardIndex])
            {
                // 在远程玩家的卡牌列表中查找选择的卡牌
                CardForTrain selectedCard = cards.Find(c => c.cardName == movesList.moves[selectedCardIndex].name);
                // 切换卡牌状态为选中状态
                selectedCard.ChangeCardState();
            }
            else
            {
                Debug.LogError("没有选择卡牌 " + selectedCardIndex);
            }
        }
    }

    // 模型收集观测
    public override void CollectObservations(VectorSensor sensor)
    {
        // 添加我方玩家状态特征
        sensor.AddObservation(stats.hp); // 玩家血量 1维
        sensor.AddObservation(stats.freezeTime); // 冻结时间 1维
        sensor.AddObservation(stats.invulnerableTime); // 无敌时间 1维
        sensor.AddObservation(stats.damageDistribution.Item1); // 剩余持续伤害事件 1维
        sensor.AddObservation(stats.damageDistribution.Item2); // 持续伤害 1维
        sensor.AddObservation(stats.damageReductionTime); // 反伤的回合数 1维
        sensor.AddObservation(stats.nextDamageMultiplier); // 下回合伤害乘区 1维
        sensor.AddObservation(stats.freezingEndDamage); // 冷冻后要结算的伤害 1维
        sensor.AddObservation(stats.currentDefense); // 当前卡牌的剩余防御力 1维
        sensor.AddObservation(stats.isLastCardDodge); // 上一次卡牌是否闪避 1维
        // 共10维

        // 添加我方的能量点信息为固定长度数组
        foreach (var point in stats.abilityPoints.Values)
        {
            sensor.AddObservation(point);
        }
        // 共13维

        // 我方卡牌的可选状态
        foreach (bool isSelectable in cardSelectability)
        {
            sensor.AddObservation(isSelectable ? 1.0f : 0.0f);
        }
        // 共74维

        // 添加对方玩家状态特征
        sensor.AddObservation(opponentPlayer.stats.hp); // 玩家血量 1维
        sensor.AddObservation(opponentPlayer.stats.freezeTime); // 冻结时间 1维
        sensor.AddObservation(opponentPlayer.stats.invulnerableTime); // 无敌时间 1维
        sensor.AddObservation(opponentPlayer.stats.damageDistribution.Item1); // 剩余持续伤害事件 1维
        sensor.AddObservation(opponentPlayer.stats.damageDistribution.Item2); // 持续伤害 1维
        sensor.AddObservation(opponentPlayer.stats.damageReductionTime); // 反伤的回合数 1维
        sensor.AddObservation(opponentPlayer.stats.nextDamageMultiplier); // 下回合伤害乘区 1维
        sensor.AddObservation(opponentPlayer.stats.freezingEndDamage); // 冷冻后要结算的伤害 1维
        sensor.AddObservation(opponentPlayer.stats.currentDefense); // 当前卡牌的剩余防御力 1维
        sensor.AddObservation(opponentPlayer.stats.isLastCardDodge); // 上一次卡牌是否闪避 1维
        // 共10维

        // 添加对方的能量点信息为固定长度数组
        foreach (var point in opponentPlayer.stats.abilityPoints.Values)
        {
            sensor.AddObservation(point);
        }
        // 共13维

        // 对方卡牌的可选状态
        foreach (bool isSelectable in opponentPlayer.cardSelectability)
        {
            sensor.AddObservation(isSelectable ? 1.0f : 0.0f);
        }
        // 共74维

        // 总计194维
    }

    // 重写动作遮罩
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // 遍历所有卡牌，设置遮罩
        for (int i = 0; i < cardSelectability.Count; i++)
        {
            // 如果卡牌不可选
            if (!cardSelectability[i])
            {
                // 使用 actionMask 的 SetActionEnabled 方法禁用动作 i
                actionMask.SetActionEnabled(0, i, false);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(selectCard == null) return;

        // 获取离散动作的数组
        var discreteActionsOut = actionsOut.DiscreteActions;

        // 遍历 cards
        int selectedIndex = -1;
        for (int i = 0; i < cards.Count; i++)
        {
            // 寻找匹配索引
            if (cards[i].cardName == selectCard.cardName)
            {
                selectedIndex = i;
                break;
            }
        }

        // 如果找到匹配的卡牌索引
        if (selectedIndex != -1)
        {
            // 设置为离散动作的第一个值
            discreteActionsOut[0] = selectedIndex;
        }
        else
        {
            // 默认值
            discreteActionsOut[0] = -1;
        }
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

    // 自动生成卡牌
    public void ReGenerateCards()
    {
        // 清除所有的卡牌
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // 清空卡牌列表和卡牌可选状态列表
        cards = new List<CardForTrain>();
        cardSelectability = new List<bool>();

        // 遍历所有招数，为每种招数生成卡牌
        foreach (var move in movesList.moves)
        {
            // 检查当前卡牌是否可以选择
            bool isSelectable = CanSelectCard(move);
            // 将卡牌的可选状态添加到列表中
            cardSelectability.Add(isSelectable);

            // 如果卡牌可选，则实例化并设置卡牌
            if (isSelectable)
            {
                GameObject card = Instantiate(cardPrefab, content.position, transform.rotation, content);
                CardForTrain cardComponent = card.GetComponent<CardForTrain>();
                if (cardComponent != null)
                {
                    cardComponent.SetupCard(move);
                    cards.Add(cardComponent);
                }
            }
        }
    }

    // 判断卡牌是否可以选择
    private bool CanSelectCard(Move move)
    {
        // 检查卡牌的每种能力点消耗是否被当前玩家状态满足
        foreach (var cost in move.abilityCost)
        {
            if (stats.abilityPoints[cost.key] < cost.value)
            {
                // 如果玩家的任一能力点不足，卡牌不可选择
                return false;
            }
        }

        // 特别检查：如果卡牌是“闪”且上一回合已出“闪”，则不可再次选择
        return !(move.name == "闪" && stats.isLastCardDodge);
    }

    // 检查卡牌选中状态
    public void CheckSelectCard(CardForTrain selectedCard)
    {
        // 遍历检查所有卡牌
        foreach (CardForTrain card in cards)
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
    public IEnumerator CenterCard(CardForTrain card)
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