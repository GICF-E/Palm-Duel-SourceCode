using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using System;
using System.Linq;
using Unity.MLAgents.Policies;

public class RefereeForTrain : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("动画控制器组件")] public Animator animator;
    [Tooltip("卡牌模型预制体")] public GameObject cardModelPrefab;

    [Header("玩家")]
    [Tooltip("我方玩家的卡牌选择")] public Transform player1CardTarget;
    [Tooltip("对方玩家的卡牌选择")] public Transform player2CardTarget;
    [Tooltip("我方玩家")] public PlayerTrain player1;
    [Tooltip("对方玩家")] public PlayerTrain player2;

    [Header("结算界面")]
    [Tooltip("胜利结算界面")] public GameObject winWindows;
    [Tooltip("胜利界面画布组")] public CanvasGroup winWindowsCanvasGroup;
    [Tooltip("胜利结算经验值文本")] public Text winSkillText;
    [Tooltip("失败界面")] public GameObject loseWindows;
    [Tooltip("失败界面画布组")] public CanvasGroup loseWindowsCanvasGroup;
    [Tooltip("失败结算经验值文本")] public Text loseSkillText;

    [Header("属性")]
    [Tooltip("是否在录制")] public bool isRecording;
    [HideInInspector][Tooltip("招数数据")] public MovesList movesList;

    private void Awake()
    {
        Debug.Log("Referee Awake");
        // 初始化
        animator = GetComponent<Animator>();
        // 初始化alpha和禁用
        winWindowsCanvasGroup.alpha = 0;
        winWindows.SetActive(false);
        winSkillText.text = "";
        loseWindowsCanvasGroup.alpha = 0;
        loseWindows.SetActive(false);
        loseSkillText.text = "";
        // 加载招数
        LoadMovesData();
    }

    private void Start()
    {
        // 当脚本启动时开始拍手和选择过程
        StartCoroutine(ClapRoutine());
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

    // 拍手
    private IEnumerator ClapRoutine()
    {
        // 等待PlayerTrain初始化
        yield return new WaitUntil(() => player1.isInitialized && player2.isInitialized);
        while (true)
        {
            // 等待PlayerTrain初始化
            yield return new WaitUntil(() => player1.isInitialized && player2.isInitialized);

            if (!isRecording)
            {
                // 请求模型决策
                player1.RequestDecision();
                player2.RequestDecision();
                // 等待双方选择卡牌并且动画播放完毕
                yield return new WaitUntil(() => player1.selectCard != null && player2.selectCard != null);
            }else{
                // 等待双方选择卡牌并且动画播放完毕
                yield return new WaitUntil(() => player1.selectCard != null && player2.selectCard != null);
                // 请求模型决策
                player1.RequestDecision();
                player2.RequestDecision();
            }

            // 初始化玩家卡牌
            Move player1move = new Move();
            Move player2move = new Move();

            // 从数据库中查找并赋值给player1move和player2move
            foreach (var move in movesList.moves)
            {
                if (move.name == player1.selectCard.cardName)
                {
                    player1move = move;
                }
                if (move.name == player2.selectCard.cardName)
                {
                    player2move = move;
                }
            }

            GameObject player1Card;
            GameObject player2Card;

            // 获取卡牌绝对位置，生成新的卡牌，并初始化
            player1Card = Instantiate(cardModelPrefab, player1.selectCard.transform.position, player1.selectCard.transform.rotation, player1CardTarget);
            player2Card = Instantiate(cardModelPrefab, player2.selectCard.transform.position, player2.selectCard.transform.rotation, player2CardTarget);

            // 初始化卡牌
            player1Card.GetComponent<CardModel>().SetupCard(player1move);
            player2Card.GetComponent<CardModel>().SetupCard(player2move);

            // 移动卡牌
            player1Card.transform.position = player1CardTarget.position;
            player2Card.transform.position = player2CardTarget.position;

            // 执行战斗逻辑
            ResolveBattle();
            // 计算奖励系统
            ModelReward(player1, player2);
            ModelReward(player2, player1);

            // 销毁卡牌
            Destroy(player1Card);
            Destroy(player2Card);

            // 重置上一次玩家状态
            player1.previousStats = player1.stats;
            player2.previousStats = player2.stats;

            // 重置选择
            if (player1.stats.freezeTime == 0)
            {
                player1.selectCard = null;
            }
            if (player2.stats.freezeTime == 0)
            {
                player2.selectCard = null;
            }
        }
    }

    // 执行战斗
    private void ResolveBattle()
    {
        // 初始化玩家移动和当前防御力
        Move player1move = new Move();
        Move player2move = new Move();

        // 从数据库中查找并赋值给player1move和player2move
        foreach (var move in movesList.moves)
        {
            if (move.name == player1.selectCard.cardName)
            {
                player1move = move;
            }
            if (move.name == player2.selectCard.cardName)
            {
                player2move = move;
            }
        }

        // 更新当前防御力
        player1.stats.currentDefense = player1move.defense;
        player2.stats.currentDefense = player2move.defense;

        // 检查冷冻并执行特殊卡牌处理
        if (player1.stats.freezeTime == 0) SpecialPropertyHandling(player1, player2, player1move, player2move);
        if (player2.stats.freezeTime == 0) SpecialPropertyHandling(player2, player1, player2move, player1move);

        // 计算两个玩家的总伤害
        float totalDamageToPlayer1 = CalculateTotalDamage(player2, player1, player2move, player1move);
        float totalDamageToPlayer2 = CalculateTotalDamage(player1, player2, player1move, player2move);

        // 抵消伤害并应用到玩家的生命值
        ApplyDamageAfterOffset(totalDamageToPlayer1, totalDamageToPlayer2);

        // 检查玩家是否死亡
        if (CheckForDeath(player1)) return;
        if (CheckForDeath(player2)) return;

        // 更新提示文本
        player1.UpdateUI();
        player2.UpdateUI();

        // 重新生成卡牌
        player1.ReGenerateCards();
        player2.ReGenerateCards();

        // 归位出闪标识符
        player1.stats.isLastCardDodge = false;
        player2.stats.isLastCardDodge = false;
    }

    // 应用伤害
    private void ApplyDamageAfterOffset(float damageToPlayer1, float damageToPlayer2)
    {
        // 计算两方实际接受的净伤害
        // 从Player1扣除的净伤害
        float netDamageToPlayer1 = damageToPlayer1 - damageToPlayer2;
        // 从Player2扣除的净伤害
        float netDamageToPlayer2 = damageToPlayer2 - damageToPlayer1;

        // 仅当计算结果为正时扣血，即伤害大于所受的反击
        if (netDamageToPlayer1 > 0)
        {
            player1.stats.hp -= netDamageToPlayer1;
        }
        if (netDamageToPlayer2 > 0)
        {
            player2.stats.hp -= netDamageToPlayer2;
        }
    }


    // 计算总伤害（反伤将被直接执行）
    private float CalculateTotalDamage(PlayerTrain attacker, PlayerTrain defender, Move attackerMove, Move defenderMove)
    {
        // 检查是否被冷冻
        if (attacker.stats.freezeTime == 0)
        {
            // 执行能力点消耗
            foreach (var ability in attackerMove.abilityCost)
            {
                // 减少总和能力点
                attacker.stats.abilityPoints["总和"] -= ability.value;

                // 判断消耗的能力点类型是否是总和或点
                if (ability.key == "总和" || ability.key == "点")
                {
                    // 减少的能力点数量，确保减少的数量等于总和或点的减少量
                    int remainingToReduce = ability.value;
                    List<string> keys;

                    if (ability.key == "总和")
                    {
                        // 获取所有非总和和非点的能力点类型
                        keys = attacker.stats.abilityPoints.Keys.Where(key => key != "总和" && key != "点").ToList();
                    }
                    else
                    {
                        // 处理点类型，初始化需要减少的具体能力点类型
                        keys = new List<string> { "加点", "扣点", "diamond" };
                        // 减少点的总和
                        attacker.stats.abilityPoints["点"] -= ability.value;
                    }

                    // 遍历其他类型的能力点
                    foreach (var key in keys)
                    {
                        if (remainingToReduce <= 0) break;

                        // 计算实际减少的数量
                        int reductionAmount = Math.Min(attacker.stats.abilityPoints[key], remainingToReduce);
                        // 减少该类型的能力点
                        attacker.stats.abilityPoints[key] -= reductionAmount;
                        // 更新剩余需要减少的数量
                        remainingToReduce -= reductionAmount;
                    }
                }
                // 如果消耗的不是"总和"或"点"
                else if (attacker.stats.abilityPoints.ContainsKey(ability.key))
                {
                    // 减少特定类型的能力点
                    attacker.stats.abilityPoints[ability.key] -= ability.value;
                    // 如果消耗点小类，即加点、扣点或diamond
                    if (ability.key == "加点" || ability.key == "扣点" || ability.key == "diamond")
                    {
                        // 减少点类能力点
                        attacker.stats.abilityPoints["点"] -= ability.value;
                    }
                }
            }

            // 执行能力点增加
            foreach (var ability in attackerMove.abilityIncrease)
            {
                // 增加特定类型的能力点
                attacker.stats.abilityPoints[ability.key] += ability.value;
                // 增加总和
                // 确保增加的不是点
                if (ability.key != "点") attacker.stats.abilityPoints["总和"] += ability.value;
            }

            // 执行加血
            attacker.stats.hp += attackerMove.heal;

            // 计算基础伤害
            float baseDamage = attackerMove.damage;

            // 计算克制加成
            float restraintBonus = CalculateRestraintBonus(attackerMove, defenderMove);

            // 计算总伤害并应用防御
            float totalDamage = baseDamage + restraintBonus;

            // 计算冷冻后执行伤害
            // 确定我方招数不是冷冻
            if (attackerMove.name != "冷冻")
            {
                // 执行伤害
                totalDamage += attacker.stats.freezingEndDamage;
                // 归位冷冻后执行伤害
                attacker.stats.freezingEndDamage = 0;
            }

            // 判断对方是否反伤
            if (defender.stats.damageReductionTime > 0)
            {
                // 计算伤害加成
                // 确定我方招数不是火药
                if (attackerMove.name != "火药")
                {
                    totalDamage *= attacker.stats.nextDamageMultiplier;
                    attacker.stats.nextDamageMultiplier = 1;
                }
                // 应用反伤到自己
                // 计算防御后剩余攻击力，闪电无视4点防御并保证不为负
                float damageReductionAfterDefense = Math.Max(0, totalDamage - Math.Max(0, defender.stats.currentDefense - (attackerMove.name == "闪电" ? 4 : 0)));
                // 减少我方防御
                attacker.stats.currentDefense = Math.Max(0, attacker.stats.currentDefense - totalDamage);
                // 清空总伤害
                totalDamage = 0;
                // 执行反伤
                attacker.stats.hp -= damageReductionAfterDefense;
                // 减少对方反伤回合数
                defender.stats.damageReductionTime--;
            }

            // 判断对方是否无敌
            if (defender.stats.invulnerableTime > 0)
            {
                // 清空总伤害（除持续伤害外）
                totalDamage = 0;
                // 减少对方无敌回合数
                defender.stats.invulnerableTime--;
            }

            // 计算持续伤害
            // 判断是否存在持续伤害
            if (attacker.stats.damageDistribution.Item1 > 0)
            {
                // 应用持续伤害
                totalDamage += defender.stats.damageDistribution.Item2;
                // 减少持续伤害回合数
                attacker.stats.damageDistribution = new Tuple<int, int>(defender.stats.damageDistribution.Item1 - 1, defender.stats.damageDistribution.Item2);
            }

            // 计算伤害加成
            // 确定我方招数不是火药
            if (attackerMove.name != "火药")
            {
                totalDamage *= attacker.stats.nextDamageMultiplier;
                attacker.stats.nextDamageMultiplier = 1;
            }

            // 计算防御后剩余攻击力，闪电无视4点防御并保证不为负
            float damageAfterDefense = Math.Max(0, totalDamage - Math.Max(0, defender.stats.currentDefense - (attackerMove.name == "闪电" ? 4 : 0)));
            // 减少对方防御
            defender.stats.currentDefense = Math.Max(0, defender.stats.currentDefense - totalDamage);
            // 返回计算后的伤害值
            return damageAfterDefense;
        }
        else
        {
            // 减少冷冻时间
            attacker.stats.freezeTime--;
            // 计算总伤害并应用防御
            float totalDamage = 0;

            // 计算持续伤害
            // 判断是否存在持续伤害
            if (defender.stats.damageDistribution.Item1 > 0)
            {
                // 应用持续伤害
                totalDamage += defender.stats.damageDistribution.Item2;
                // 减少持续伤害回合数
                defender.stats.damageDistribution = new Tuple<int, int>(defender.stats.damageDistribution.Item1 - 1, defender.stats.damageDistribution.Item2);
            }

            // 计算伤害加成
            // 确定我方招数不是火药
            if (attackerMove.name != "火药")
            {
                totalDamage *= attacker.stats.nextDamageMultiplier;
                attacker.stats.nextDamageMultiplier = 1;
            }

            // 计算防御后剩余攻击力，闪电无视防御并保证不为负
            float damageAfterDefense = Math.Max(0, totalDamage - defender.stats.currentDefense);
            // 减少对方防御
            defender.stats.currentDefense = Math.Max(0, defender.stats.currentDefense - totalDamage);

            // 返回计算后的伤害值
            return damageAfterDefense;
        }
    }

    // 计算克制加成
    private float CalculateRestraintBonus(Move attackerMove, Move defenderMove)
    {
        foreach (var restraint in attackerMove.restrain)
        {
            if (restraint.key == defenderMove.name)
            {
                // 如果找到克制关系，返回额外的伤害加成
                return restraint.value;
            }
        }
        // 没有克制关系
        return 0;
    }

    // 检查玩家是否死亡
    private bool CheckForDeath(PlayerTrain player)
    {
        // 如果玩家生命值小于等于0
        if (player.stats.hp <= 0)
        {
            // 死亡
            player.stats.hp = 0;
            Debug.Log(player.name + " is dead");
            // 根据胜负情况显示结算界面
            if (player == player1)
            {
                StartCoroutine(HandleGameEnd(false));
            }
            else if (player == player2)
            {
                StartCoroutine(HandleGameEnd(true));
            }
            return true;
        }
        return false;
    }

    // 结算游戏结果
    private IEnumerator HandleGameEnd(bool isWin)
    {
        // 获取对应的 CanvasGroup 和界面系统
        CanvasGroup targetCanvasGroup = isWin ? winWindowsCanvasGroup : loseWindowsCanvasGroup;
        GameObject targetWindow = isWin ? winWindows : loseWindows;
        Text skillText = isWin ? winSkillText : loseSkillText;

        // 启用对应的结算界面
        targetWindow.SetActive(true);
        skillText.text = "Skill";
        // 显示界面
        targetCanvasGroup.alpha = 1;

        yield return null;

        // 重置界面
        targetCanvasGroup.alpha = 0;
        targetWindow.SetActive(false);

        // 结束训练
        player1.EndEpisode();
        player2.EndEpisode();
    }

    // 处理模型奖励系统
    private void ModelReward(PlayerTrain player, PlayerTrain opponent)
    {
        // 累积奖励变量
        float totalReward = 0f;

        // 如果我方死亡
        if (player.stats.hp <= 0)
        {
            // 应用负面奖励
            totalReward += -2f;
            player.AddReward(-2f);
        }
        else if (opponent.stats.hp <= 0)
        {
            // 应用正面奖励
            totalReward += 2f;
            player.AddReward(2f);
        }
        else
        {
            // 计算我方血量变化量
            float playerHPChange = player.stats.hp - player.previousStats.hp;
            // 应用奖励
            if (playerHPChange != 0)
            {
                totalReward += playerHPChange * 0.3f;
                player.AddReward(playerHPChange * 0.3f);
            }

            // 计算对方血量变化量
            float opponentHPChange = opponent.stats.hp - opponent.previousStats.hp;
            // 应用奖励
            if (opponentHPChange != 0)
            {
                totalReward += -opponentHPChange * 0.3f;
                player.AddReward(-opponentHPChange * 0.3f);
            }

            // 冻结时间的奖励机制
            if (opponent.stats.freezeTime > opponent.previousStats.freezeTime)
            {
                totalReward += opponent.stats.freezeTime * 0.2f;
                player.AddReward(opponent.stats.freezeTime * 0.2f);
            }
            if (player.stats.freezeTime > player.previousStats.freezeTime)
            {
                totalReward += -player.stats.freezeTime * 0.2f;
                player.AddReward(-player.stats.freezeTime * 0.2f);
            }

            // 无敌时间的奖励机制
            if (player.stats.invulnerableTime > player.previousStats.invulnerableTime)
            {
                totalReward += player.stats.invulnerableTime * 0.2f;
                player.AddReward(player.stats.invulnerableTime * 0.2f);
            }

            // 反伤的奖励机制
            if (player.stats.damageReductionTime > player.previousStats.damageReductionTime)
            {
                totalReward += player.stats.damageReductionTime * 0.2f;
                player.AddReward(player.stats.damageReductionTime * 0.2f);
            }
        }

        // 记录累计的团队奖励
        Academy.Instance.StatsRecorder.Add("Team" + player.GetComponent<BehaviorParameters>().TeamId + "/Reward", totalReward, StatAggregationMethod.Average);
    }

    // 特殊卡牌处理
    public void SpecialPropertyHandling(PlayerTrain attacker, PlayerTrain defender, Move attackerMove, Move defenderMove)
    {
        switch (attackerMove.name)
        {
            case "闪":
                Card_Dodge(attacker);
                break;
            case "湮灭":
                Card_Annihilate(defender);
                break;
            case "满血复活":
                Card_FullBloodResurrection(attacker);
                break;
            case "点Chee":
                Card_DotChee(defender, defenderMove);
                break;
            case "眩晕电击枪":
                Card_StunLightning(defender, defenderMove);
                break;
            case "核电站":
                Card_NuclearPowerStation(attacker);
                break;
            case "流沙陷阱":
                Card_QuicksandTrap(attacker);
                break;
            case "火药":
                Card_Gunpowder(attacker);
                break;
            case "鱼跃龙门":
                Card_CarpLeap(attacker);
                break;
            case "冷冻":
                Card_Freeze(attacker, defender, attackerMove, defenderMove);
                break;
            default:
                break;
        }
    }

    // 特殊卡牌效果方法
    // 闪
    private void Card_Dodge(PlayerTrain attacker)
    {
        // 标记已经出过闪
        attacker.stats.isLastCardDodge = true;
    }

    // 湮灭
    private void Card_Annihilate(PlayerTrain defender)
    {
        // 对手直接死亡，返回最大生命值作为伤害
        defender.stats.hp = int.MinValue;
    }

    // 满血复活
    private void Card_FullBloodResurrection(PlayerTrain attacker)
    {
        // 增加无敌时间
        attacker.stats.invulnerableTime = 5;
    }

    // 点Chee
    private void Card_DotChee(PlayerTrain defender, Move defenderMove)
    {
        // 判断对方是否出拳头
        if (defenderMove.name == "拳头" && defender.stats.freezeTime == 0)
        {
            // 执行冻结
            defender.stats.freezeTime += 2;
            // 增加能量点
            defender.stats.abilityPoints["拳头"] += 1;
        }
    }

    // 炫电
    private void Card_StunLightning(PlayerTrain defender, Move defenderMove)
    {
        // 判断对方是否出闪
        if (defenderMove.name == "闪")
        {
            // 执行冻结
            defender.stats.freezeTime += 3;
        }
    }

    // 核电站
    private void Card_NuclearPowerStation(PlayerTrain attacker)
    {
        // 增加持续伤害
        attacker.stats.damageDistribution = new Tuple<int, int>(attacker.stats.damageDistribution.Item1 + 2, 10);
    }

    // 流沙陷阱
    private void Card_QuicksandTrap(PlayerTrain attacker)
    {
        // 增加反伤时间
        attacker.stats.damageReductionTime += 5;
    }

    // 火药
    private void Card_Gunpowder(PlayerTrain attacker)
    {
        // 设置下次伤害乘区
        attacker.stats.nextDamageMultiplier = 1.5f;
    }

    // 鱼跃龙门
    private void Card_CarpLeap(PlayerTrain attacker)
    {
        // 增加无敌时间
        attacker.stats.invulnerableTime += 5;
    }

    // 冷冻
    private void Card_Freeze(PlayerTrain attacker, PlayerTrain defender, Move attackerMove, Move defenderMove)
    {
        // 计算当前自己会收到的伤害
        // 计算基础伤害
        float baseDamage = defenderMove.damage;

        // 计算克制加成
        float restraintBonus = CalculateRestraintBonus(defenderMove, attackerMove);

        // 计算总伤害并应用防御
        float totalDamage = baseDamage + restraintBonus;

        // 判断对方是否反伤
        if (attacker.stats.damageReductionTime > 0)
        {
            // 清空总伤害
            totalDamage = 0;
        }

        // 判断对方是否无敌
        if (defender.stats.invulnerableTime > 0)
        {
            // 清空总伤害（除持续伤害外）
            totalDamage = 0;
        }

        // 计算持续伤害
        // 判断是否存在持续伤害
        if (defender.stats.damageDistribution.Item1 > 0)
        {
            // 应用持续伤害
            totalDamage += defender.stats.damageDistribution.Item2;
        }

        // 计算伤害加成
        // 确定我方招数不是火药
        if (attackerMove.name != "火药")
        {
            totalDamage *= attacker.stats.nextDamageMultiplier;
        }

        // 存储冷冻后伤害
        defender.stats.freezingEndDamage = totalDamage;
        // 增加敌人的冷冻时间
        defender.stats.freezeTime += 3;
    }
}