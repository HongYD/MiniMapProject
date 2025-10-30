using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [SerializeField]
    private GameObject minimapIconPivot;// Image：小地图图标容器（UI 节点）
    [SerializeField]
    private RawImage minimapRawImage;// 小地图相机渲染到的 RenderTexture 的展示控件

    [SerializeField]
    private Button buttonUp;
    [SerializeField]
    private Button buttonDown;

    private Camera miniMapCamera;// 小地图相机（正交）

    GameObject playerIcon;
    Transform playerTrans; // 世界坐标系下的人物 Transform

    Vector3 centerPosMiniMap; // 小地图中心点坐标（UI）

    Vector2 xSizeUI; // 小地图 UI 在 x轴上的像素范围 [min,max]
    Vector2 ySizeUI; // 小地图 UI 在 y轴上的像素范围 [min,max]

    float cameraViewHalfSize;// 小地图相机正交尺寸的一半（OrthographicSize）

    float mainCamRotY;// 主相机旋转欧拉角的 Y 分量（偏航）

    private List<MinimapObj> minimapObjs;

    private void Start()
    {
        // 初始化数据结构
        minimapObjs = new List<MinimapObj>();
        miniMapCamera = GameObject.Find("MiniMapCam").gameObject.GetComponent<Camera>();
        playerTrans = GameObject.Find("PlayerArmature").gameObject.transform;

        // 将人物图标设置到小地图 UI 的中心点，并设置到容器下
        playerIcon = Instantiate(Resources.Load<GameObject>("Prefabs/PlayerIcon"));
        playerIcon.transform.SetParent(minimapIconPivot.transform, false);
        playerIcon.GetComponent<RectTransform>().localPosition = centerPosMiniMap;

        //读取小地图中心（UI 节点的锚点位置）
        float posX = minimapIconPivot.GetComponent<RectTransform>().anchoredPosition.x;
        float posY = minimapIconPivot.GetComponent<RectTransform>().anchoredPosition.y;
        centerPosMiniMap = new Vector2(posX, posY);

        //计算小地图 UI 的矩形范围（像素）
        RectTransform mapTrans = this.GetComponent<RectTransform>();
        xSizeUI = new Vector2(-mapTrans.sizeDelta.x /2, mapTrans.sizeDelta.x /2);
        ySizeUI = new Vector2(-mapTrans.sizeDelta.y /2, mapTrans.sizeDelta.y /2);

        cameraViewHalfSize = miniMapCamera.orthographicSize; // 初始半视口大小

        mainCamRotY = Camera.main.transform.eulerAngles.y; // 初始主相机偏航角

        // 注册需要显示在小地图上的对象
        RegisterMinimapObjects();

        //绑定缩放按钮
        buttonUp.onClick.AddListener(OnClickButtonUp);
        buttonDown.onClick.AddListener(OnClickButtonDown);
    }

    private void Update()
    {
        cameraViewHalfSize = miniMapCamera.orthographicSize; // 每帧读取小地图相机的半视口尺寸（决定世界→UI 的线性映射范围）
        //计算人和物体的坐标时，都以小地图相机为基准（其视空间的 x=右、y=上，顶视时约等于世界 X/Z）

        //1)计算玩家图标的旋转（玩家面朝与主相机正前之间的夹角，投影到 XZ 平面）
        Vector3 cameraLookAt = Camera.main.transform.forward; // 主相机朝向（世界）
        Vector3 playerLocalForward = playerTrans.transform.forward; // 玩家朝向（世界）
        float angleY = Vector2.SignedAngle(
            new Vector2(cameraLookAt.x, cameraLookAt.z),
            new Vector2(playerLocalForward.x, playerLocalForward.z)
        ); // 从相机前到玩家前的有符号夹角（逆时针为正）
        playerIcon.GetComponent<RectTransform>().rotation = Quaternion.Euler(0,0, angleY); // 将角度设置到玩家图标的 Z轴旋转

        // 把玩家世界坐标变换到“小地图相机的视空间”（注意：这不是 Viewport[0..1]，而是 View Space）
        Vector3 viewportPlayerPos = miniMapCamera.worldToCameraMatrix.MultiplyPoint(playerTrans.position);

        //2) 遍历每个注册对象，计算其在小地图上的 UI位置
        for (int i =0; i < minimapObjs.Count; i++)
        {
            //物体的视空间位置（以小地图相机为坐标系）
            Vector3 viewportPos = miniMapCamera.worldToCameraMatrix.MultiplyPoint(minimapObjs[i].owner.transform.position);

            //物体相对于玩家的视空间位移（只关心平面：x=右，y=上；忽略 z）
            Vector3 relativePosViewport = viewportPos - viewportPlayerPos;

            // 玩家与物体的距离（当前用三维距离，包含 view.z）
            // 如果只需平面距离更稳定，可改为：float distToPlayer = new Vector2(relativePosViewport.x, relativePosViewport.y).magnitude
            // //•	之所以是“以 +y 轴为零度”，是因为你调用了 Mathf.Atan2(relativePosViewport.x, relativePosViewport.y)，即把 x 当作第1参(y)、y 当作第2参(x)。这样对于向上向量 (0,1) 得到 0°，零度轴就在 +y。           
            float distToPlayer = Vector3.Distance(viewportPlayerPos, viewportPos);

            //角度对齐：
            // φy = atan2(x, y)（以 +y轴为零度、逆时针为正的角度）；
            //也就是说relativePosViewport这个坐标空间就是以+y为零度的，逆时针为正角的空间
            // 将 φy 转成以 +x 为零度的角并与主相机偏航对齐：δ = yaw - φy +90°。
            float deltaY = Camera.main.transform.eulerAngles.y - Mathf.Atan2(relativePosViewport.x, relativePosViewport.y) * Mathf.Rad2Deg +90.0f;

            // 用极坐标还原到笛卡尔坐标（以 +x 为零度）：x = r·cosδ, y = r·sinδ
            relativePosViewport.x = distToPlayer * Mathf.Cos(deltaY * Mathf.Deg2Rad);
            relativePosViewport.y = distToPlayer * Mathf.Sin(deltaY * Mathf.Deg2Rad);

            // 将视空间的 x/y线性映射到 UI 像素范围：[-S, +S] → [xMin, xMax] / [yMin, yMax]
            float rateX = Mathf.Clamp01((relativePosViewport.x - (-cameraViewHalfSize)) / (cameraViewHalfSize *2)); //归一化到[0,1]
            float xUIPos = xSizeUI.x + (xSizeUI.y - xSizeUI.x) * rateX; // 插值到 UI 像素范围

            float rateY = Mathf.Clamp01((relativePosViewport.y - (-cameraViewHalfSize)) / (cameraViewHalfSize *2)); //归一化到[0,1]
            float yUIPos = ySizeUI.x + (ySizeUI.y - ySizeUI.x) * rateY; // 插值到 UI 像素范围

            //赋值到对应图标的 RectTransform
            Vector2 miniMapPos = new Vector2(xUIPos, yUIPos);
            minimapObjs[i].icon.transform.SetParent(minimapIconPivot.transform); // 保证在容器下
            minimapObjs[i].icon.gameObject.GetComponent<RectTransform>().localScale = Vector3.one; //统一缩放
            minimapObjs[i].icon.gameObject.GetComponent<RectTransform>().localPosition = miniMapPos; // 设置 UI位置
        }

        // 当主相机朝向变化超过阈值时，旋转底图使“小地图上方”与主相机前对齐
        // 注意：这里先使用旧的 mainCamRotY 设置旋转，再更新它为当前值，可能有1 帧延迟；
        // 如需即时跟随，可直接使用当前的 Camera.main.transform.eulerAngles.y。
        if (Mathf.Abs(mainCamRotY - Camera.main.transform.eulerAngles.y) >1.0f)
        {
            minimapRawImage.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0,0, mainCamRotY);
            mainCamRotY = Camera.main.transform.eulerAngles.y;
        }
    }


    private void RegisterMinimapObjects()
    {
        // 注册所有地图图标（以 Buildings 下的子物体为例）
        Transform buildingPivots = GameObject.Find("Buildings").transform;
        int childCount = buildingPivots.childCount;

        for (int i =0; i < childCount; i++)
        {
            GameObject go = Resources.Load<GameObject>($"Prefabs/BuildingIcon");
            RegisterSingleMinimapObject(buildingPivots.GetChild(i).gameObject, go);
        }
    }

    private void RegisterSingleMinimapObject(GameObject o, GameObject i)
    {
        GameObject icon = Instantiate(i);
        minimapObjs.Add(new MinimapObj() { owner = o, icon = icon });
    }

    private void OnClickButtonUp()
    {
        miniMapCamera.orthographicSize +=2.0f; // 放大视场（看到更大范围）
    }

    private void OnClickButtonDown()
    {
        miniMapCamera.orthographicSize -=2.0f; // 缩小视场（看到更小范围）
    }
}
