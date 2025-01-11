using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using Newtonsoft.Json;

public class Signup : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("邮箱输入框")] public InputField email;
    [Tooltip("用户名输入框")] public InputField displayName;
    [Tooltip("密码输入框")] public InputField password;
    [Tooltip("确认密码输入框")] public InputField confirmPassword;
    [Tooltip("警告文本")] public Text warningText;
    [Tooltip("UI画布组")] public CanvasGroup signupCanvasGroup;
    [Tooltip("开始游戏界面")] public GameObject startCanvas;

    [Header("属性")]
    [Tooltip("淡出时间")] public float fadeDuration = 0.5f;
    [Tooltip("UI淡化曲线")] public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        // 隐藏警告文本
        warningText.text = "";
        // 隐藏UI
        signupCanvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        // 启动游戏时淡入UI
        StartCoroutine(FadeInUI(fadeDuration));
    }

    // 注册账号
    public void Register()
    {
        // 检查输入框内容是否为空
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(displayName.text) || string.IsNullOrEmpty(password.text) || string.IsNullOrEmpty(confirmPassword.text))
        {
            // 更新提示词
            warningText.text = "所有字段都必须填写";
            return;
        }

        // 检查密码和确认密码是否匹配
        if (password.text != confirmPassword.text)
        {
            // 更新提示词
            warningText.text = "密码和确认密码不匹配";
            return;
        }

        // 构造注册请求
        var request = new RegisterPlayFabUserRequest
        {
            Email = email.text,
            Password = password.text,
            DisplayName = displayName.text,
            RequireBothUsernameAndEmail = false
        };

        // 发送注册请求
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccessAndUpdateUserData, OnError);
    }


    // 注册成功
    private void OnRegisterSuccessAndUpdateUserData(RegisterPlayFabUserResult result)
    {
        // 构造更新用户数据请求
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {"Skill", "20"},
                {"AttendedGames", "0"},
                {"WinGames", "0"},
                {"HistoryRecords", JsonConvert.SerializeObject(new List<(string, string)>())}
            }
        };

        // 发送更新用户数据请求
        PlayFabClientAPI.UpdateUserData(request, OnDataUpdated, OnError);
    }

    // 更新用户数据成功
    private void OnDataUpdated(UpdateUserDataResult result)
    {
        Debug.Log("更新用户数据成功");
        // 更新提示词
        warningText.text = "注册成功";
        // 清空输入框
        email.text = "";
        displayName.text = "";
        password.text = "";
        confirmPassword.text = "";
    }

    // 执行失败
    private void OnError(PlayFabError error)
    {
        // 定义错误信息
        string errorMessage;

        // 检查错误代码并更新错误信息
        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidEmailAddress:
                errorMessage = "电子邮件地址无效";
                break;
            case PlayFabErrorCode.EmailAddressNotAvailable:
                errorMessage = "电子邮件地址已被使用";
                break;
            case PlayFabErrorCode.InvalidUsername:
                errorMessage = "用户名无效";
                break;
            case PlayFabErrorCode.UsernameNotAvailable:
                errorMessage = "用户名已被使用";
                break;
            case PlayFabErrorCode.InvalidPassword:
                errorMessage = "密码无效";
                break;
            case PlayFabErrorCode.ServiceUnavailable:
                errorMessage = "服务不可用";
                break;
            default:
                errorMessage = "发生错误，请稍后再试";
                break;
        }

        Debug.LogError("注册失败: " + error.GenerateErrorReport());

        // 更新提示词
        warningText.text = errorMessage;
        // 清空输入框
        email.text = "";
        displayName.text = "";
        password.text = "";
        confirmPassword.text = "";
    }

    // 退出至开始界面
    public void Exit()
    {
        // 开始淡出UI的协程
        StartCoroutine(FadeOutAndLoadCanvas(startCanvas, fadeDuration));
    }

    // 等待动画播放完成并淡出UI，然后切换场景
    private IEnumerator FadeOutAndLoadCanvas(GameObject canvas, float fadeOutDuration)
    {
        // 开始淡出UI
        yield return StartCoroutine(FadeOutUI(fadeOutDuration));

        // 等待动画播放完成
        yield return new WaitForSeconds(fadeOutDuration);

        // 启用指定界面
        canvas.SetActive(true);
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
            signupCanvasGroup.alpha = alpha;
            // 等待下一帧
            yield return null;
        }

        // 确保最终alpha值为0
        signupCanvasGroup.alpha = 0f;
    }

    // 淡入UI透明度的协程
    private IEnumerator FadeInUI(float fadeInDuration)
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
            signupCanvasGroup.alpha = alpha;
            // 等待下一帧
            yield return null;
        }

        // 确保最终alpha值为1
        signupCanvasGroup.alpha = 1f;
    }
}
