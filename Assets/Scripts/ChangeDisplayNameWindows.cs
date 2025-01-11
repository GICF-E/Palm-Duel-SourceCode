using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

public class ChangeDisplayNameWindows : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("邮箱输入框")] public InputField email;
    [Tooltip("用户名输入框")] public InputField displayName;
    [Tooltip("警告文本")] public Text warningText;

    private void OnEnable()
    {
        // 隐藏警告文本
        warningText.text = "";
    }

    // 登录账号
    public void ChangeDisplayName()
    {
        // 检查输入框内容是否为空
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(displayName.text))
        {
            // 更新提示词
            warningText.text = "所有字段都必须填写";
            return;
        }

        // 检查邮件地址是否正确
        if (email.text != UserData.playerEmail)
        {
            // 更新提示词
            warningText.text = "邮箱地址不正确";
            return;
        }

        // 构造登录请求
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = displayName.text
        };

        // 发送登录请求
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnUpdateSuccess, OnError);
    }

    // 登录成功
    private void OnUpdateSuccess(UpdateUserTitleDisplayNameResult result)
    {
        // 更新提示词
        warningText.text = "修改成功";
        // 清空UserData数据
        UserData.ResetData();
        // 切换到主场景
        SceneManager.LoadScene("Main");
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
                errorMessage = "邮箱无效";
                break;
            case PlayFabErrorCode.AccountNotFound:
                errorMessage = "账号未找到";
                break;
            case PlayFabErrorCode.AccountBanned:
                errorMessage = "账号被封禁";
                break;
            case PlayFabErrorCode.InvalidUsernameOrPassword:
                errorMessage = "用户无效";
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

    // 返回方法
    public void Cancel()
    {
        // 返回主场景
        SceneManager.LoadScene("Main");
    }
}