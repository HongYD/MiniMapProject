using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [SerializeField]
    private GameObject minimapIconPivot;//Image
    [SerializeField]
    private RawImage minimapRawImage;//相机渲染的Raw Image

    [SerializeField]
    private Button buttonUp;
    [SerializeField]
    private Button buttonDown;

    private Camera miniMapCamera;//小地图相机

    GameObject playerIcon;
    Transform playerTrans; //世界坐标系下的人物Transform

    Vector3 centerPosMiniMap; //小地图中心点坐标

    Vector2 xSizeUI; //相机视口坐标x
    Vector2 ySizeUI; //相机视口坐标y

    float cameraViewHalfSize;// 相机视口大小的一半

    float mainCamRotY;//主相机旋转欧拉角的Y分量

    private List<MinimapObj> minimapObjs;

    private void Start()
    {
        //初始化
        minimapObjs = new List<MinimapObj>();
        miniMapCamera = GameObject.Find("MiniMapCam").gameObject.GetComponent<Camera>();
        playerTrans = GameObject.Find("PlayerArmature").gameObject.transform;

        //将人物图标设置到小地图UI的中心点，并将其父类设置到minimapIconPivot
        playerIcon = Instantiate(Resources.Load<GameObject>("Prefabs/PlayerIcon"));
        playerIcon.transform.SetParent(minimapIconPivot.transform, false);
        playerIcon.GetComponent<RectTransform>().localPosition = centerPosMiniMap;


        //小地图中心坐标
        float posX = minimapIconPivot.GetComponent<RectTransform>().anchoredPosition.x;
        float posY = minimapIconPivot.GetComponent<RectTransform>().anchoredPosition.y;
        centerPosMiniMap = new Vector2(posX, posY);

        //小地图UI坐标的最大值与最小值
        RectTransform mapTrans = this.GetComponent<RectTransform>();
        xSizeUI = new Vector2(-mapTrans.sizeDelta.x / 2, mapTrans.sizeDelta.x / 2);
        ySizeUI = new Vector2(-mapTrans.sizeDelta.y / 2, mapTrans.sizeDelta.y / 2);

        cameraViewHalfSize = miniMapCamera.orthographicSize;

        mainCamRotY = Camera.main.transform.eulerAngles.y;

        RegisterMinimapObjects();

        buttonUp.onClick.AddListener(OnClickButtonUp);
        buttonDown.onClick.AddListener(OnClickButtonDown);
    }

    private void Update()
    {
        cameraViewHalfSize = miniMapCamera.orthographicSize;


        //1.获取人物图标旋转的Y
        Vector3 cameraLookAt = Camera.main.transform.forward;//相机照射方向
        Vector3 playerLocalForward = playerTrans.transform.forward;//人物面向
        float angleY = Vector2.SignedAngle(new Vector2(cameraLookAt.x, cameraLookAt.z), new Vector2(playerLocalForward.x, playerLocalForward.z));//人物面向相对于相机面向夹角
        playerIcon.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleY);//将人物图标旋转这个角度

        Vector3 viewportPlayerPos = miniMapCamera.worldToCameraMatrix.MultiplyPoint(playerTrans.position);//人物在小地图相机视口坐标系下的坐标

        //2.计算每个注册的地图信息相对于人物的旋转和位置，是在小地图相机的视口坐标系下，最后将视口坐标系的坐标映射到UI坐标系下
        for (int i=0;i< minimapObjs.Count; i++)
        {
            Vector3 viewportPos = miniMapCamera.worldToCameraMatrix.MultiplyPoint(minimapObjs[i].owner.transform.position);
            //计算相对于人的位置
            Vector3 relativePosViewport = viewportPos - viewportPlayerPos;
            float distToPlayer = Vector3.Distance(viewportPlayerPos, viewportPos);
            float deltaY = Camera.main.transform.eulerAngles.y - Mathf.Atan2(relativePosViewport.x, relativePosViewport.y) * Mathf.Rad2Deg + 90;

            relativePosViewport.x = distToPlayer * Mathf.Cos(deltaY * Mathf.Deg2Rad);//三角函数
            relativePosViewport.y = distToPlayer * Mathf.Sin(deltaY * Mathf.Deg2Rad);
            float rateX = Mathf.Clamp01((relativePosViewport.x - (-cameraViewHalfSize)) / (cameraViewHalfSize * 2));
            float xUIPos = xSizeUI.x + (xSizeUI.y - xSizeUI.x) * rateX;
            float rateY = Mathf.Clamp01((relativePosViewport.y - (-cameraViewHalfSize)) / (cameraViewHalfSize * 2));
            float yUIPos = ySizeUI.x + (ySizeUI.y - ySizeUI.x) * rateY;
            Vector2 miniMapPos = new Vector2(xUIPos, yUIPos);//获得UI坐标
            minimapObjs[i].icon.transform.SetParent(minimapIconPivot.transform);//设置父类
            minimapObjs[i].icon.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
            minimapObjs[i].icon.gameObject.GetComponent<RectTransform>().localPosition = miniMapPos;//赋值
        }

        if (Mathf.Abs(mainCamRotY - Camera.main.transform.eulerAngles.y) > 1.0f)
        {
            minimapRawImage.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, mainCamRotY);
            mainCamRotY = Camera.main.transform.eulerAngles.y;
        }
    }


    private void RegisterMinimapObjects()
    {
        //注册所有地图图标
        Transform buildingPivots = GameObject.Find("Buildings").transform;
        int childCount = buildingPivots.childCount;

        for (int i = 0; i < childCount; i++)
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
        miniMapCamera.orthographicSize += 2.0f;
    }

    private void OnClickButtonDown()
    {
        miniMapCamera.orthographicSize -= 2.0f;
    }
}
