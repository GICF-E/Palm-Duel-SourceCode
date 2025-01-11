using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("邮箱输入框")] public InputField email;
    [Tooltip("密码输入框")] public InputField password;
    [Tooltip("警告文本")] public Text warningText;
    [Tooltip("UI画布组")] public CanvasGroup loginCanvasGroup;
    [Tooltip("开始游戏界面")] public GameObject startCanvas;

    [Header("属性")]
    [Tooltip("淡出时间")] public float fadeDuration = 0.5f;
    [Tooltip("UI淡化曲线")] public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void OnEnable()
    {
        // 隐藏警告文本
        warningText.text = "";
        // 隐藏UI
        loginCanvasGroup.alpha = 0f;
        // 尝试自动登录
        AttemptAutoLogin();
    }

    // 尝试自动登录
    public void AttemptAutoLogin()
    {
        // 检查PlayerPrefs中是否有保存的CustomID
        if (PlayerPrefs.HasKey("CustomID"))
        {
            // 获取保存的CustomID
            string customId = PlayerPrefs.GetString("CustomID");

            // 尝试使用CustomID自动登录
            var request = new LoginWithCustomIDRequest
            {
                CustomId = customId,
                CreateAccount = false
            };

            // 发送登录请求
            PlayFabClientAPI.LoginWithCustomID(request, OnAutoLoginSuccess, OnAutoLoginFailure);
        }
        else
        {
            // 没有保存的CustomID，直接显示登录界面
            StartCoroutine(FadeInUI(fadeDuration));
        }
    }

    // 自动登录成功
    private void OnAutoLoginSuccess(LoginResult result)
    {
        // 检查游戏版本并切换到主场景
        CheckVersionAndLoadScene("Main", false);
    }

    // 自动登录失败
    private void OnAutoLoginFailure(PlayFabError error)
    {
        // 显示登录界面
        StartCoroutine(FadeInUI(fadeDuration));
    }

    // 登录账号
    public void LogInAccount()
    {
        // 检查输入框内容是否为空
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text))
        {
            // 更新提示词
            warningText.text = "所有字段都必须填写";
            return;
        }

        // 构造登录请求
        var request = new LoginWithEmailAddressRequest
        {
            Email = email.text,
            Password = password.text
        };

        // 发送登录请求
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    // 登录成功
    private void OnLoginSuccess(LoginResult result)
    {
        // 检查是否已经保存了CustomID
        if (!PlayerPrefs.HasKey("CustomID"))
        {
            // 生成并保存一个唯一的CustomID到PlayerPrefs
            Debug.Log("没有保存CustomID，生成并保存一个唯一的CustomID到PlayerPrefs");
            string customId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("CustomID", customId);
            PlayerPrefs.Save();

            // 将CustomID关联到PlayFab账户
            var linkRequest = new LinkCustomIDRequest
            {
                CustomId = customId,
                ForceLink = true
            };
            PlayFabClientAPI.LinkCustomID(linkRequest, result => Debug.Log("CustomID关联成功"), error => Debug.LogError("CustomID关联失败"));
        }

        // 检查游戏版本并切换到主场景
        CheckVersionAndLoadScene("Main", true);
    }

    // 检查游戏版本并切换场景
    private void CheckVersionAndLoadScene(string sceneName, bool isFadeOut)
    {
        // 获取标题数据
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), result =>
        {
            if (result.Data.TryGetValue("LatestVersion", out string latestVersion))
            {
                Debug.Log($"当前版本: {Application.version}, 最新版本: {latestVersion}");
                // 如果版本不匹配
                if (Application.version != latestVersion)
                {
                    Debug.LogWarning("版本不匹配，需要更新");
                    SceneManager.LoadScene("Upgrade");
                }else{
                    // 切换场景
                    if(isFadeOut) StartCoroutine(FadeOutAndLoadScene(sceneName, fadeDuration));
                    else SceneManager.LoadScene(sceneName);
                }
            }
            else
            {
                Debug.LogError("未找到LatestVersion");
            }
        }, error =>
        {
            Debug.LogError($"获取标题数据失败：{error.GenerateErrorReport()}");
        });
    }

    // 忘记密码
    public void ForgotPassword(){
        // 检查是否输入了邮箱
        if (string.IsNullOrEmpty(email.text))
        {
            // 更新提示词
            warningText.text = "请先输入电子邮件";
            return;
        }

        // 构造请求参数
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = email.text,
            TitleId = PlayFabSettings.TitleId,
        };

        // 发送密码重置邮件
        PlayFabClientAPI.SendAccountRecoveryEmail(request, result => warningText.text = "密码重置邮件已经发送到您的邮箱", OnError);
    }

    // 登录失败
    private void OnError(PlayFabError error)
    {
        // 定义错误信息
        string errorMessage;

        // 检查错误代码并更新错误信息
        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidEmailOrPassword:
                errorMessage = "邮箱或密码无效";
                break;
            case PlayFabErrorCode.AccountNotFound:
                errorMessage = "账号未找到";
                break;
            case PlayFabErrorCode.AccountBanned:
                errorMessage = "账号被封禁";
                break;
            case PlayFabErrorCode.InvalidUsernameOrPassword:
                errorMessage = "用户名或密码无效";
                break;
            case PlayFabErrorCode.ConnectionError:
                errorMessage = "网络连接错误";
                break;
            default:
                errorMessage = "发生错误，请稍后重试";
                break;
        }

        // 更新提示词
        warningText.text = errorMessage;
    }

    // 退出至开始界面
    public void Exit()
    {
        // 开始淡出UI的协程
        StartCoroutine(FadeOutAndLoadCanvas(startCanvas, fadeDuration));
    }

    // 等待动画播放完成并淡出UI，然后切换界面
    private IEnumerator FadeOutAndLoadCanvas(GameObject canvas, float fadeOutDuration)
    {
        // 开始淡出UI
        yield return StartCoroutine(FadeOutUI(fadeOutDuration));

        // 等待动画播放完成
        yield return new WaitForSeconds(fadeOutDuration);

        // 启用指定界面
        canvas.SetActive(true);
    }

    // 等待动画播放完成并淡出UI，然后切换场景
    private IEnumerator FadeOutAndLoadScene(string scene, float fadeOutDuration)
    {
        // 开始淡出UI
        yield return StartCoroutine(FadeOutUI(fadeOutDuration));

        // 等待动画播放完成
        yield return new WaitForSeconds(fadeOutDuration);

        // 切换到指定场景
        SceneManager.LoadScene(scene);
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
            loginCanvasGroup.alpha = alpha;
            // 等待下一帧
            yield return null;
        }

        // 确保最终alpha值为0
        loginCanvasGroup.alpha = 0f;
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
            loginCanvasGroup.alpha = alpha;
            // 等待下一帧
            yield return null;
        }

        // 确保最终alpha值为1
        loginCanvasGroup.alpha = 1f;
    }
}