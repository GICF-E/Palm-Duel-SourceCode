using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TextButton : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("文本组件")] public Text buttonText;

    [Header("属性")]
    [Tooltip("正常状态颜色")] public Color normalColor;
    [Tooltip("高亮颜色")] public Color highlightColor;
    [Tooltip("按下时的颜色")] public Color pressedColor;
    [Tooltip("颜色变化持续时间")] public float colorChangeDuration;
    [Tooltip("颜色变化曲线")] public AnimationCurve colorChangeCurve;
    [Tooltip("当前正在进行的颜色渐变协程")] private Coroutine currentColorChange;

    void Start()
    {
        // 设置初始颜色
        buttonText.color = normalColor;
    }

    // 鼠标进入时触发
    public void OnPointerEnter()
    {
        // 高亮颜色
        ChangeTextColor(highlightColor);
    }

    // 鼠标按下时触发
    public void OnPointerDown()
    {
        // 按下颜色
        ChangeTextColor(pressedColor);
    }

    // 鼠标抬起时触发
    public void OnPointerUp()
    {
        // 高亮颜色
        ChangeTextColor(highlightColor);
    }

    // 鼠标移出时触发
    public void OnPointerExit()
    {
        // 正常颜色
        ChangeTextColor(normalColor);
    }

    // 改变文本颜色，使用动画曲线进行渐变
    private void ChangeTextColor(Color targetColor)
    {
        // 如果有正在进行的颜色渐变，停止它
        if (currentColorChange != null)
        {
            StopCoroutine(currentColorChange);
        }

        // 启动新的颜色渐变协程
        currentColorChange = StartCoroutine(ChangeColorRoutine(buttonText.color, targetColor));
    }

    // 颜色渐变协程
    private IEnumerator ChangeColorRoutine(Color startColor, Color endColor)
    {
        float elapsedTime = 0f;

        while (elapsedTime < colorChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / colorChangeDuration;
            // 使用动画曲线调整进度 t 的值
            t = colorChangeCurve.Evaluate(t);
            buttonText.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        // 确保最终颜色设置为目标颜色
        buttonText.color = endColor;
        currentColorChange = null;
    }
}