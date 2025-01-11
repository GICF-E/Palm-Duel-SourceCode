using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Linq;
using Newtonsoft.Json;

public class RefereeForAgent : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("动画控制器组件")] public Animator animator;
    [Tooltip("卡牌模型预制体")] public GameObject cardModelPrefab;

    [Header("玩家")]
    [Tooltip("我方玩家的卡牌选择")] public Transform player1CardTarget;
    [Tooltip("对方玩家的卡牌选择")] public Transform player2CardTarget;
    [Tooltip("我方玩家")] public PlayerAgent player1;
    [Tooltip("对方玩家")] public PlayerAgent player2;

    [Header("结算界面")]
    [Tooltip("结算界面显示动画持续时间")] public float resuelFadeInDuration;
    [Tooltip("结算界面显示动画曲线")] public AnimationCurve resuelFadeInCurve;
    [Tooltip("等待n秒和返回主场景")] public float resuelWaitDuration;
    [Tooltip("胜利结算界面")] public GameObject winWindows;
    [Tooltip("胜利界面画布组")] public CanvasGroup winWindowsCanvasGroup;
    [Tooltip("胜利结算经验值文本")] public Text winSkillText;
    [Tooltip("失败界面")] public GameObject loseWindows;
    [Tooltip("失败界面画布组")] public CanvasGroup loseWindowsCanvasGroup;
    [Tooltip("失败结算经验值文本")] public Text loseSkillText;

    [Header("移动动画")]
    [Tooltip("移动动画的持续时间")] public float moveDuration;
    [Tooltip("移动动画曲线")] public AnimationCurve moveCurve;

    [Header("缩小动画")]
    [Tooltip("缩小动画的持续时间")] public float shrinkDuration;
    [Tooltip("缩小动画曲线")] public AnimationCurve shrinkCurve;

    [Header("属性")]
    [Tooltip("排行榜名称")] public string leaderboardName = "SkillLeaderboards";
    [HideInInspector][Tooltip("招数数据")] public MovesList movesList;

    private void Awake()
    {
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
        // 等待初始动画结束
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
        while (player1.stats.hp > 0 && player2.stats.hp > 0)
        {
            // 执行拍手动画
            animator.SetTrigger("clap");
            // 模型决策
            player2.RequestDecision();
            // 等待选择卡牌并且动画播放完毕
            yield return new WaitUntil(() => player1.selectCard != null && player2.selectCard != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f);

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

            // 创建列表以用于存储卡牌移动的协程
            List<Coroutine> moveCoroutines = new List<Coroutine>();

            // 同时移动两张卡牌
            if (player1.stats.freezeTime == 0)
            {
                moveCoroutines.Add(StartCoroutine(MoveCard(player1Card, player1Card.transform.position, player1CardTarget.position, moveDuration)));
            }
            if (player2.stats.freezeTime == 0)
            {
                moveCoroutines.Add(StartCoroutine(MoveCard(player2Card, player2Card.transform.position, player2CardTarget.position, moveDuration)));
            }

            // 等待所有卡牌移动完成
            foreach (var coroutine in moveCoroutines)
            {
                yield return coroutine;
            }

            // 执行战斗逻辑
            ResolveBattle();

            // 等待0.8秒
            yield return new WaitForSeconds(0.8f);

            // 清空列表
            moveCoroutines.Clear();

            // 同时移动和缩小卡牌然后销毁
            if (player1.stats.freezeTime == 0)
            {
                moveCoroutines.Add(StartCoroutine(MoveAndShrinkCard(player1Card, transform.position, shrinkDuration, shrinkCurve)));
            }
            if (player2.stats.freezeTime == 0)
            {
                moveCoroutines.Add(StartCoroutine(MoveAndShrinkCard(player2Card, transform.position, shrinkDuration, shrinkCurve)));
            }

            // 等待所有卡牌缩小完成
            foreach (var coroutine in moveCoroutines)
            {
                yield return coroutine;
            }

            // 销毁卡牌
            Destroy(player1Card);
            Destroy(player2Card);

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

    // 移动卡牌
    private IEnumerator MoveCard(GameObject card, Vector3 startPosition, Vector3 targetPosition, float duration)
    {
        // 计时器和起始位置
        float currentTime = 0;
        card.transform.position = startPosition;

        // 循环直到计时器结束
        while (currentTime < duration)
        {
            // 计算动画的进度
            currentTime += Time.deltaTime;
            float t = currentTime / duration;
            // 使用动画曲线调整插值参数
            t = moveCurve.Evaluate(t);
            // 在初始和目标位置之间进行插值
            card.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        // 确保精确设置到目标位置
        card.transform.position = targetPosition;
    }

    // 移动卡牌并缩小
    private IEnumerator MoveAndShrinkCard(GameObject card, Vector3 targetPosition, float duration, AnimationCurve curve)
    {
        // 计时器和起始位置
        float currentTime = 0;
        Vector3 startPosition = card.transform.position;
        Vector3 startScale = card.transform.localScale;
        Quaternion startRotation = card.transform.rotation;

        // 计算目标旋转，以面对Referee的位置
        Vector3 directionToReferee = targetPosition - startPosition;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, directionToReferee);

        // 循环直到计时器结束
        while (currentTime < duration)
        {
            // 计算动画的进度
            currentTime += Time.deltaTime;
            float t = currentTime / duration;
            // 使用动画曲线调整插值参数
            t = curve.Evaluate(t);
            // 在初始和目标位置之间进行插值
            card.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            // 缩小卡牌
            card.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            // 旋转卡牌
            card.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }
        // 确保精确设置到目标位置
        card.transform.position = targetPosition;
        card.transform.localScale = Vector3.zero;
        card.transform.rotation = targetRotation;
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
    private float CalculateTotalDamage(PlayerAgent attacker, PlayerAgent defender, Move attackerMove, Move defenderMove)
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
    private bool CheckForDeath(PlayerAgent player)
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
        // 计算自己要改变的经验值
        int skillChange = isWin ? 0 : -0;

        // 验证数据一致性
        yield return StartCoroutine(VerifyUserData(isVerified =>
        {
            if (isVerified)
            {
                // 加入历史记录到 List
                if (UserData.historyRecords.Count >= 30)
                {
                    // 从尾部移除最旧的记录
                    UserData.historyRecords.RemoveAt(UserData.historyRecords.Count - 1);
                }
                // 将新记录添加到头部
                UserData.historyRecords.Insert(0, ("Sirius", isWin ? $"+{Mathf.Abs(skillChange)}" : $"-{Mathf.Abs(skillChange)}"));

                // 更新经验值并提交
                UserData.UpdatePlayerSkill(UserData.GetPlayerSkill() + skillChange);

                StartCoroutine(UpdateLocalPlayerData(isWin));
                StartCoroutine(SubmitSkillToLeaderboard());
            }
            else
            {
                Debug.LogError("双重数据验证失败");
                // 封号逻辑
                BanPlayer(UserData.playerPlayFabId, "数据作弊", 120);
                // 切换到封号场景
                SceneManager.LoadScene("Ban");
            }
        }));

        // 获取对应的 CanvasGroup 和界面系统
        CanvasGroup targetCanvasGroup = isWin ? winWindowsCanvasGroup : loseWindowsCanvasGroup;
        GameObject targetWindow = isWin ? winWindows : loseWindows;
        Text skillText = isWin ? winSkillText : loseSkillText;

        // 启用对应的结算界面
        targetWindow.SetActive(true);
        skillText.text = "Skill" + (isWin ? " + " + Mathf.Abs(skillChange) : " - " + Mathf.Abs(skillChange));

        // 逐步显示界面，基于编辑器中设置的动画曲线和持续时间
        float elapsedTime = 0f;
        while (elapsedTime < resuelFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / resuelFadeInDuration;
            // 使用动画曲线调整 alpha 值
            t = resuelFadeInCurve.Evaluate(t);
            targetCanvasGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        // 确保 alpha 设为 1
        targetCanvasGroup.alpha = 1;

        // 等待指定的时间后返回主场景
        yield return new WaitForSeconds(resuelWaitDuration);

        // 清空UserData
        UserData.ResetData();

        // 切换到主场景
        SceneManager.LoadScene("Main");
    }

    // 更新本地玩家云端经验值
    private IEnumerator UpdateLocalPlayerData(bool isWin)
    {
        // 最大重试次数
        int retries = 3;
        // 每次重试前的等待时间
        float retryDelay = 1f;

        for (int attempt = 0; attempt < retries; attempt++)
        {
            bool isComplete = false;

            // 发起更新本地玩家数据请求
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { "Skill", UserData.GetPlayerSkill().ToString() },
                    { "AttendedGames", (UserData.playerAttendedGames + 1).ToString() },
                    { "WinGames", (isWin ? UserData.playerWinGames + 1 : UserData.playerWinGames).ToString() },
                    { "HistoryRecords", JsonConvert.SerializeObject(UserData.historyRecords) }
                }
            };

            PlayFabClientAPI.UpdateUserData(request, result =>
            {
                Debug.Log("本地玩家数据更新成功");
                // 标记操作完成并成功
                isComplete = true;
            }, error =>
            {
                Debug.LogError($"本地玩家数据更新失败: {error.GenerateErrorReport()}. 重试 {attempt + 1}/{retries}");
                // 标记操作完成但失败
                isComplete = true;
            });

            // 等待更新操作完成
            yield return new WaitUntil(() => isComplete);

            // 如果更新成功，结束协程
            if (isComplete)
            {
                yield break;
            }

            // 如果失败且未达到最大重试次数，则等待后重试
            if (attempt < retries - 1)
            {
                yield return new WaitForSeconds(retryDelay);
            }
        }

        Debug.LogError("多次尝试更新失败，放弃操作。");
    }

    // 提交经验值到排行榜
    private IEnumerator SubmitSkillToLeaderboard()
    {
        // 最大重试次数
        int retries = 3;
        // 每次重试前的等待时间
        float retryDelay = 1f;

        for (int attempt = 0; attempt < retries; attempt++)
        {
            bool isComplete = false;

            // 构造更新玩家统计的请求
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = leaderboardName,
                        Value = UserData.GetPlayerSkill()
                    }
                }
            };

            // 提交经验值到排行榜
            PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
            {
                Debug.Log("经验值提交到排行榜成功");
                // 标记操作完成并成功
                isComplete = true;
            }, error =>
            {
                Debug.LogError($"经验值提交到排行榜失败: {error.GenerateErrorReport()}. 重试 {attempt + 1}/{retries}");
                // 标记操作完成但失败
                isComplete = true;
            });

            // 等待提交操作完成
            yield return new WaitUntil(() => isComplete);

            // 如果提交成功，结束协程
            if (isComplete)
            {
                yield break;
            }

            // 如果失败且未达到最大重试次数，则等待后重试
            if (attempt < retries - 1)
            {
                yield return new WaitForSeconds(retryDelay);
            }
        }

        Debug.LogError("多次尝试提交经验值到排行榜失败，放弃操作。");
    }

    // 验证数据一致性
    public IEnumerator VerifyUserData(Action<bool> callback)
    {
        bool isVerified = false, isComplete = false;

        // 获取服务器数据
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data.ContainsKey("Skill") && result.Data.ContainsKey("HistoryRecords"))
            {
                // 解析数据
                int serverSkill = int.Parse(result.Data["Skill"].Value);
                var serverHistory = JsonConvert.DeserializeObject<List<(string, string)>>(result.Data["HistoryRecords"].Value);

                // 验证数据一致性
                isVerified = serverSkill == UserData.GetPlayerSkill() && JsonConvert.SerializeObject(serverHistory) == JsonConvert.SerializeObject(UserData.historyRecords);
                isComplete = true;
            }
            else
            {
                Debug.LogError("服务器数据缺失");
                // 标记为验证成功，避免误封
                isVerified = true;
                isComplete = true;
            }
        }, error =>
        {
            Debug.LogError($"验证用户数据失败：{error.GenerateErrorReport()}");
            // 标记为验证成功，避免误封
            isVerified = true;
            isComplete = true;
        });

        // 等待回调完成
        yield return new WaitUntil(() => isComplete);

        // 返回验证结果
        callback?.Invoke(isVerified);
    }

    // 调用云脚本封禁玩家
    public void BanPlayer(string playFabId, string reason, int? durationInHours)
    {
        // 构造请求并调用 ExecuteCloudScript
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                // 云脚本的函数名
                FunctionName = "banPlayer",
                FunctionParameter = new
                {
                    PlayFabId = playFabId,
                    Reason = reason,
                    DurationInHours = durationInHours
                },
                // 生成 PlayStream 事件
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                Debug.Log("成功调用云脚本");
            },
            error =>
            {
                Debug.LogError("调用云脚本错误: " + error.GenerateErrorReport());
            }
        );
    }

    // 特殊卡牌处理
    public void SpecialPropertyHandling(PlayerAgent attacker, PlayerAgent defender, Move attackerMove, Move defenderMove)
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
    private void Card_Dodge(PlayerAgent attacker)
    {
        // 标记已经出过闪
        attacker.stats.isLastCardDodge = true;
    }

    // 湮灭
    private void Card_Annihilate(PlayerAgent defender)
    {
        // 对手直接死亡，返回最大生命值作为伤害
        defender.stats.hp = int.MinValue;
    }

    // 满血复活
    private void Card_FullBloodResurrection(PlayerAgent attacker)
    {
        // 增加无敌时间
        attacker.stats.invulnerableTime = 5;
    }

    // 点Chee
    private void Card_DotChee(PlayerAgent defender, Move defenderMove)
    {
        // 判断对方是否出拳头
        if (defenderMove.name == "拳头" && defender.stats.freezeTime <= 1)
        {
            // 执行冻结
            defender.stats.freezeTime += 2;
            // 增加能量点
            defender.stats.abilityPoints["拳头"] += 1;
        }
    }

    // 炫电
    private void Card_StunLightning(PlayerAgent defender, Move defenderMove)
    {
        // 判断对方是否出闪
        if (defenderMove.name == "闪")
        {
            // 执行冻结
            defender.stats.freezeTime += 3;
        }
    }

    // 核电站
    private void Card_NuclearPowerStation(PlayerAgent attacker)
    {
        // 增加持续伤害
        attacker.stats.damageDistribution = new Tuple<int, int>(attacker.stats.damageDistribution.Item1 + 2, 10);
    }

    // 流沙陷阱
    private void Card_QuicksandTrap(PlayerAgent attacker)
    {
        // 增加反伤时间
        attacker.stats.damageReductionTime += 5;
    }

    // 火药
    private void Card_Gunpowder(PlayerAgent attacker)
    {
        // 设置下次伤害乘区
        attacker.stats.nextDamageMultiplier = 1.5f;
    }

    // 鱼跃龙门
    private void Card_CarpLeap(PlayerAgent attacker)
    {
        // 增加无敌时间
        attacker.stats.invulnerableTime += 5;
    }

    // 冷冻
    private void Card_Freeze(PlayerAgent attacker, PlayerAgent defender, Move attackerMove, Move defenderMove)
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