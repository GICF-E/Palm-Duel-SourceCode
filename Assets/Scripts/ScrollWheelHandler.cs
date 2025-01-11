using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 将鼠标滚轮事件映射到ScrollView
/// </summary>
public class ScrollWheelHandler : MonoBehaviour, IScrollHandler
{
    private ScrollRect scrollRect;

    // 自动初始化
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    public void OnScroll(PointerEventData data)
    {
        // 直接调用ScrollRect的滚动处理
        scrollRect.OnScroll(data);
    }
}