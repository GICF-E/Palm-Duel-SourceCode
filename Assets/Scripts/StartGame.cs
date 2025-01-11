using System.Collections;
using UnityEngine;

public class StartGame : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("Referee动画器")] public Animator animator;
    [Tooltip("启动界面UI画布组")] public CanvasGroup startCanvasGroup;
    [Tooltip("Login界面")] public GameObject loginWindow;
    [Tooltip("Signup界面")] public GameObject signupWindow;

    [Header("属性")]
    [Tooltip("UI淡出时间")] public float fadeOutDuration;
    [Tooltip("UI淡化曲线")] public AnimationCurve fadeCurve;

    private void OnEnable()
    {
        // 禁用Signup和Login界面
        loginWindow.SetActive(false);
        signupWindow.SetActive(false);
        // 获取当前动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // 计算动画剩余时间
        float animationDuration = stateInfo.length;
        // 启动游戏时淡入UI
        StartCoroutine(FadeInUI(animationDuration, startCanvasGroup));
    }

    // 登陆界面
    public void Login()
    {
        // 播放Referee退出动画
        animator.SetTrigger("exit");
        // 启动协程等待动画播放完成并淡出UI
        StartCoroutine(FadeOutAndLoadCanvas(loginWindow));
    }

    // 注册界面
    public void Signup()
    {
        // 播放Referee退出动画
        animator.SetTrigger("exit");
        // 启动协程等待动画播放完成并淡出UI
        StartCoroutine(FadeOutAndLoadCanvas(signupWindow));
    }

    // 等待动画播放完成并淡出UI，然后切换界面
    private IEnumerator FadeOutAndLoadCanvas(GameObject canvas)
    {
        // 开始淡出UI
        yield return StartCoroutine(FadeOutUI(fadeOutDuration));

        // 启用指定界面前禁用其他界面
        loginWindow.SetActive(false);
        signupWindow.SetActive(false);

        // 启用指定界面
        canvas.SetActive(true);

        // 禁用自己
        gameObject.SetActive(false);
    }

    // 淡出UI透明度的协程
    private IEnumerator FadeOutUI(float fadeOutDuration)
    {
        // 已经过的时间
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            // 增加经过的时间
            elapsedTime += Time.deltaTime;
            // 计算非线性的alpha值，使用动画曲线实现非线性效果
            float normalizedTime = elapsedTime / fadeOutDuration;
            float alpha = 1 - fadeCurve.Evaluate(normalizedTime);
            // 设置CanvasGroup的alpha值
            startCanvasGroup.alpha = alpha;
            // 等待下一帧
            yield return null;
        }

        // 确保最终alpha值为0
        startCanvasGroup.alpha = 0f;
    }

    // 淡入UI透明度的协程
    private IEnumerator FadeInUI(float fadeInDuration, CanvasGroup canvasGroup)
    {
        // 已经过的时间
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            // 增加经过的时间
            elapsedTime += Time.deltaTime;
            // 计算非线性的alpha值，使用动画曲线实现非线性效果
            float normalizedTime = elapsedTime / fadeInDuration;
            float alpha = fadeCurve.Evaluate(normalizedTime);
            // 设置CanvasGroup的alpha值
            canvasGroup.alpha = alpha;
            // 等待下一帧
            yield return null;
        }

        // 确保最终alpha值为1
        canvasGroup.alpha = 1f;
    }
}