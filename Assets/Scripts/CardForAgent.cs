using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CardForAgent : MonoBehaviour
{
    [Header("缩放动画属性")]
    [Tooltip("鼠标进入时的放大百分比")] public float pointerEnterScale = 1.2f;
    [Tooltip("放大动画的持续时间")] public float scaleDuration = 0.2f;
    [Tooltip("缩小动画的持续时间")] public float shrinkDuration = 0.2f;
    [Tooltip("缩放动画的曲线")] public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("缩放动画的协程")] private Coroutine scaleCoroutine;

    [Header("变色动画属性")]
    [Tooltip("卡牌边缘")] public Image cardEdge;
    [Tooltip("基本颜色")] public Color normalColor = Color.white;
    [Tooltip("选择变色后的颜色")] public Color targetColor = Color.grey;
    [Tooltip("颜色变化动画的持续时间")] public float colorChangeDuration = 0.2f;
    [Tooltip("颜色变化动画的曲线")] public AnimationCurve colorChangeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("组件引用")]
    [Tooltip("玩家对象实例")] public PlayerAgent player;
    [Tooltip("Button 组件")] public Button button;
    [Tooltip("Image 组件")] public Image cardImage;
    [Tooltip("卡牌名字")] public Text cardText;
    [Tooltip("卡牌伤害或回复")] public Text hintText1;
    [Tooltip("卡牌防御")] public Text hintText2;

    [Header("基本属性")]
    [Tooltip("卡牌是否已经被选择")] public bool isSelect = false;
    [Tooltip("卡牌的名字")] public string cardName = null;

    private void Awake()
    {
        // 获取 Player 组件
        player = GetComponentInParent<PlayerAgent>();
    }

    private void Start()
    {
        // 初始化颜色
        cardEdge.color = normalColor;
        isSelect = false;
    }

    // 当鼠标进入UI元素时调用
    public void OnPointerEnter()
    {
        // 判断是否是本地玩家
        if (!player.isPlayer) return;
        // 停止之前的动画
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        // 播放放大动画
        scaleCoroutine = StartCoroutine(ScaleOverTime(new Vector3(1, 1, 1) * pointerEnterScale, scaleDuration));
    }

    // 当鼠标退出UI元素时调用
    public void OnPointerExit()
    {
        // 判断是否是本地玩家
        if (!player.isPlayer) return;
        // 停止之前的动画
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        // 播放缩小动画
        scaleCoroutine = StartCoroutine(ScaleOverTime(new Vector3(1, 1, 1), shrinkDuration));
    }

    // 初始化卡牌方法
    public void SetupCard(Move move)
    {
        // 更新卡牌名字
        cardName = move.name;
        // 更新UI显示
        cardText.text = cardName;
        // 根据是否是本地玩家来禁用按钮组件
        button.enabled = player.isPlayer;

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

    // 切换卡牌状态
    public void ChangeCardState()
    {
        // 停止之前的动画
        StopAllCoroutines();
        // 播放颜色变化动画
        StartCoroutine(ChangeColorOverTime(cardEdge, isSelect ? normalColor : targetColor, colorChangeDuration));
        // 判断是否是从未选中状态切换成选中状态
        if (!isSelect)
        {
            // 置中卡牌
            StartCoroutine(player.CenterCard(this));
            // 更新选中卡牌
            player.selectCard = this;
            // 暂停卡牌交互
            if(player.isPlayer) StartCoroutine(player.PauseCardSelection());
        }
        // 判断是否从选中状态切换到未选中状态并且选中卡牌就是当前卡牌
        else if (player.selectCard == this && isSelect)
        {
            // 令选中卡牌为空
            player.selectCard = null;
        }
        // 切换卡牌状态
        isSelect = !isSelect;
    }

    // 缩放动画
    private IEnumerator ScaleOverTime(Vector3 targetScale, float duration)
    {
        // 计时器和起始大小
        float timer = 0f;
        Vector3 startScale = transform.localScale;

        // 循环直到计时器结束
        while (timer < duration)
        {
            // 计算缩放动画的进度
            float progress = scaleCurve.Evaluate(timer / duration);
            // 根据缩放动画的进度更新大小
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            // 更新计时器
            timer += Time.deltaTime;
            // 等待一帧
            yield return null;
        }
        // 确保最终大小准确
        transform.localScale = targetScale;
    }

    // 颜色变化动画
    private IEnumerator ChangeColorOverTime(Image image, Color targetColor, float duration)
    {
        // 计时器和起始颜色
        float timer = 0f;
        Color startColor = image.color;

        // 循环直到计时器结束
        while (timer < duration)
        {
            // 计算颜色变化动画的进度
            float progress = colorChangeCurve.Evaluate(timer / duration);
            // 根据颜色变化动画的进度更新颜色
            image.color = Color.Lerp(startColor, targetColor, progress);
            // 更新计时器
            timer += Time.deltaTime;
            // 等待一帧
            yield return null;
        }
        // 确保最终颜色准确
        image.color = targetColor;
    }
}