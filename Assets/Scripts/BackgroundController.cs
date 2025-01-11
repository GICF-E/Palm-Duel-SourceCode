using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [Tooltip("背景预制件")] public GameObject backgroundPrefab;
    [Tooltip("水平滚动速度")] public float scrollSpeedX = 0.25f;
    [Tooltip("垂直滚动速度")] public float scrollSpeedY = 0.25f;
    // 引用主摄像机
    private Camera mainCamera;
    // 屏幕边界在世界坐标的表示
    private Vector2 screenBounds;
    // 背景对象的宽度
    private float objectWidth;
    // 背景对象的高度
    private float objectHeight;
    // 需要的水平方向复制数量
    private int neededCopiesX;
    // 需要的垂直方向复制数量
    private int neededCopiesY;
    // 上一帧的屏幕宽度
    private int lastScreenWidth;
    // 上一帧的屏幕高度
    private int lastScreenHeight;

    void Start()
    {
        // 初始化主摄像机和屏幕尺寸变化的跟踪
        mainCamera = Camera.main;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        // 初始化背景
        InitializeBackground();
    }

    /// <summary>
    /// 初始化背景，创建足够的背景片段以覆盖整个屏幕和额外区域
    /// </summary>
    void InitializeBackground()
    {
        // 清除所有子对象以防重复
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 计算屏幕边界到世界坐标的转换
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        GameObject obj = Instantiate(backgroundPrefab);
        obj.transform.SetParent(transform, false);
        SpriteRenderer childSprite = obj.GetComponent<SpriteRenderer>();
        objectWidth = childSprite.bounds.size.x;
        objectHeight = childSprite.bounds.size.y;
        
        // 计算需要在每个方向上复制的背景数量
        neededCopiesX = (int)Mathf.Ceil(screenBounds.x * 2 / objectWidth) + 1;
        neededCopiesY = (int)Mathf.Ceil(screenBounds.y * 2 / objectHeight) + 1;
        
        // 设置背景片段的初始位置，从屏幕外开始
        Vector3 startPosition = new Vector3(mainCamera.transform.position.x - screenBounds.x - objectWidth / 2, mainCamera.transform.position.y - screenBounds.y - objectHeight / 2, 0);
        
        // 在两个方向上创建足够的背景片段
        for (int i = 0; i <= neededCopiesX; i++)
        {
            for (int j = 0; j <= neededCopiesY; j++)
            {
                GameObject clone = Instantiate(obj, new Vector3(startPosition.x + objectWidth * i, startPosition.y + objectHeight * j, 0), Quaternion.identity, transform);
            }
        }
        
        // 销毁最初的预制件实例，因为已经复制了所需的片段
        Destroy(obj);
    }

    void Update()
    {
        // 检查屏幕分辨率是否改变，如果改变，则重新初始化背景
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            InitializeBackground();
        }

        // 根据滚动速度移动背景
        Vector3 movement = new Vector3(-scrollSpeedX * Time.deltaTime, -scrollSpeedY * Time.deltaTime, 0);
        transform.Translate(movement);

        // 当背景片段移出屏幕时，重新定位到对面以实现无缝滚动
        foreach (Transform child in transform)
        {
            if (child.position.x < mainCamera.transform.position.x - screenBounds.x - objectWidth)
            {
                child.position = new Vector3(child.position.x + objectWidth * (neededCopiesX + 1), child.position.y, child.position.z);
            }
            if (child.position.y < mainCamera.transform.position.y - screenBounds.y - objectHeight)
            {
                child.position = new Vector3(child.position.x, child.position.y + objectHeight * (neededCopiesY + 1), child.position.z);
            }
        }
    }
}
