using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;

public class SettingManager : MonoBehaviour
{
    [Header("设置属性引用")]
    [Tooltip("电子邮件文本")] public Text emailText;
    [Tooltip("用户名文本")] public Text displayNameText;
    [Tooltip("PlayFabI GUID文本")] public Text GUIDText;
    [Tooltip("经验值文本")] public Text skillText;
    [Tooltip("总对战场次文本")] public Text attendedGamesText;
    [Tooltip("胜利场次文本")] public Text winGamesText;
    [Tooltip("修改密码的提示文本")] public Text warningText;

    [Header("动画属性")]
    [Tooltip("设置界面")] public Transform settingWindows;
    [Tooltip("设置界面收起位置")] public Transform retractionPosition;
    [Tooltip("设置界面展开位置")] public Transform expansionPosition;
    [Tooltip("遮罩面板")] public GameObject maskPanel;
    [Tooltip("遮罩面板画布组")] public CanvasGroup maskPanelCanvasGroup;
    [Tooltip("动画持续时间")] public float duration;
    [Tooltip("淡入淡出曲线")] public AnimationCurve fadeCurve;
    [Tooltip("移动曲线")] public AnimationCurve moveCurve; 

    // 私有变量，标识设置界面是否已经打开
    private bool isSettingsOpen = false;

    private void Awake() 
    {
        // 初始化界面和变量
        maskPanelCanvasGroup.alpha = 0;
        maskPanel.SetActive(false);
        isSettingsOpen = false;
        emailText.text = "";
        displayNameText.text = "";
        GUIDText.text = "";
        skillText.text = "";
        attendedGamesText.text = "";
        winGamesText.text = "";
        warningText.text = "";

        // 更新UserData数据到UI
        StartCoroutine(UpdateUserData());
    }

    // 切换账号方法
    public void SwitchAccount()
    {
        // 清空PlayerPrefs数据
        PlayerPrefs.DeleteAll();
        // 清空UserData数据
        UserData.ResetData();
        // 跳转到登录界面
        SceneManager.LoadScene("Start");
    }

    // 修改用户名方法
    public void ChangeDisplayName()
    {
        // 切换到修改用户名场景
        SceneManager.LoadScene("ChangeDisplayName");
    }

    // 修改密码方法
    public void ChangePassword()
    {
        // 构造请求参数
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = UserData.playerEmail,
            TitleId = PlayFabSettings.TitleId,
        };

        // 发送密码重置邮件
        PlayFabClientAPI.SendAccountRecoveryEmail(request, result => warningText.text = "密码重置邮件已经发送到您的邮箱", error => warningText.text = "发生错误，请稍后再试");
    }

    // 打开设置界面
    public void OpenSettings()
    {
        // 如果界面已经打开直接返回
        if (isSettingsOpen) return;

        // 启用遮罩面板
        maskPanel.SetActive(true);
        maskPanelCanvasGroup.alpha = 0;

        // 启动淡入遮罩面板和移动设置界面的协程
        StartCoroutine(FadeMaskPanel(0, 1)); 
        StartCoroutine(MoveWindow(settingWindows, retractionPosition.position, expansionPosition.position));

        // 更新状态标识，标识设置界面已打开
        isSettingsOpen = true;
    }

    // 关闭设置界面
    public void CloseSettings()
    {
        // 如果界面已经关闭直接返回
        if (!isSettingsOpen) return;

        // 启动淡出遮罩面板和移动设置界面的协程，淡出完成后禁用遮罩面板
        StartCoroutine(FadeMaskPanel(1, 0, () => maskPanel.SetActive(false)));
        StartCoroutine(MoveWindow(settingWindows, expansionPosition.position, retractionPosition.position));

        // 清空警告文本
        warningText.text = "";

        // 更新状态标识，标识设置界面已关闭
        isSettingsOpen = false;
    }

    // 控制遮罩面板的透明度变化
    private IEnumerator FadeMaskPanel(float startAlpha, float endAlpha, System.Action onComplete = null)
    {
        // 记录经过的时间
        float elapsedTime = 0f;

        // 逐步改变 CanvasGroup 的透明度
        while (elapsedTime < duration)
        {
            // 增加时间
            elapsedTime += Time.deltaTime;
            // 计算动画进度
            float t = elapsedTime / duration;
            // 从曲线中获取当前进度的值
            float curveValue = fadeCurve.Evaluate(t);
            // 使用曲线值插值计算透明度
            maskPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            yield return null;
        }

        // 确保最终的透明度值准确设置为结束透明度
        maskPanelCanvasGroup.alpha = endAlpha;
        // 如果存在完成回调，则调用
        onComplete?.Invoke();
    }

    // 控制设置界面的移动动画
    private IEnumerator MoveWindow(Transform window, Vector3 startPos, Vector3 endPos, System.Action onComplete = null)
    {
        // 记录经过的时间
        float elapsedTime = 0f;

        // 使用协程逐步改变窗口位置
        while (elapsedTime < duration)
        {
            // 增加时间
            elapsedTime += Time.deltaTime;
            // 计算动画进度
            float t = elapsedTime / duration;
            // 从曲线中获取当前进度的值，实现非线性变化
            float curveValue = moveCurve.Evaluate(t);
            // 使用曲线值插值计算位置
            window.position = Vector3.Lerp(startPos, endPos, curveValue);
            yield return null;
        }

        // 确保窗口最终位置准确
        window.position = endPos;
        // 如果存在完成回调，则调用
        onComplete?.Invoke();
    }

    // 更新 UserData 数据到 UI
    private IEnumerator UpdateUserData()
    {
        // 等待获取电子邮件信息
        yield return new WaitUntil(() => !string.IsNullOrEmpty(UserData.playerEmail));
        emailText.text = UserData.playerEmail;

        // 等待获取用户名信息
        yield return new WaitUntil(() => !string.IsNullOrEmpty(UserData.playerDisplayName));
        displayNameText.text = UserData.playerDisplayName;

        // 等待获取PlayFabID
        yield return new WaitUntil(() => !string.IsNullOrEmpty(UserData.playerPlayFabId));
        GUIDText.text = UserData.playerPlayFabId;

        // 等待获取经验值
        yield return new WaitUntil(() => UserData.playerSkill != null);
        skillText.text = UserData.GetPlayerSkill().ToString();

        // 等待获取总对战场次
        yield return new WaitUntil(() => UserData.playerAttendedGames != -1);
        attendedGamesText.text = UserData.playerAttendedGames.ToString();

        // 等待获取总胜场
        yield return new WaitUntil(() => UserData.playerWinGames != -1);
        winGamesText.text = UserData.playerWinGames.ToString();
    }
}