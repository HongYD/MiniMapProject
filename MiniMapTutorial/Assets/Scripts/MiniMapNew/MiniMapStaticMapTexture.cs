using System;
using UnityEngine;
using UnityEngine.UI;

public static class Vector3Extensions
{
    public static Vector2 ToVector2(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }
}

public enum ZoomLevel
{
    LevelM25 = 25,   // 0.25
    LevelM50= 50,   // 0.5
    LevelM75 = 75,   // 0.75
    LevelP1 = 100,  // 1
    LevelP2 = 200,  // 2
}

public class MiniMapStaticMapTexture : MonoBehaviour
{
    [SerializeField]
    private Image miniMapImage;
    [SerializeField]
    private GameObject minimapIconPivot;
    [SerializeField]
    private GameObject playerArmatureIcon;
    [SerializeField]
    private float deadZoneSize;
    [SerializeField]
    private Button buttonZoomIn;
    [SerializeField]
    private Button buttonZoomOut;
    [SerializeField]
    private ZoomLevel zoomLevel;
    public float ImageSize;

    // 插值速度与地图偏移系数（与原逻辑一致：Lerp * 5，地图中心偏移 -scaledPos / 2）
    [SerializeField]
    private float lerpSpeed = 5.0f;
    private const float MapOffsetFactor = 0.5f;

    private GameObject playerIcon;
    private RectTransform miniMapRect;
    private RectTransform playerIconRect;

    private GameObject playerObj;
    private Vector3 originPos;

    private float scaleRateX;
    private float scaleRateY;
    private bool isOverDeadZone;
    private float currentCamAspect;

    private void OnEnable()
    {
        if (!ValidateReferences()) return;
        zoomLevel = ZoomLevel.LevelP1;

        // 与原逻辑保持一致的参数赋值
        scaleRateX = 9.0f;
        scaleRateY = 16.0f;
        currentCamAspect = 1.78f;
        ImageSize = 512f;

        InitializePlayerIcon();
        CacheRectTransforms();

        buttonZoomIn.onClick.AddListener(() => {
            if (zoomLevel < ZoomLevel.LevelP2)
            {
                zoomLevel += 25;
                float xx = ImageSize * ((int)zoomLevel) / 100f;
                float yy = ImageSize * ((int)zoomLevel) / 100f;
                miniMapRect.sizeDelta = new Vector2(xx, yy);
            }
        });

        buttonZoomOut.onClick.AddListener(() => {
            if (zoomLevel > ZoomLevel.LevelM25)
            {
                zoomLevel -= 25;
                float xx = ImageSize * ((int)zoomLevel) / 100f;
                float yy = ImageSize * ((int)zoomLevel) / 100f;
                miniMapRect.sizeDelta = new Vector2(xx, yy);
            }
        });

        originPos = Vector3.zero;

        // 初始位置与小地图偏移
        Vector2 scaledPos = GetScaledPlayerPos();
        isOverDeadZone = IsBeyondDeadZone(scaledPos);

        if (isOverDeadZone)
        {
            miniMapRect.anchoredPosition = (-scaledPos * MapOffsetFactor);
            playerIconRect.anchoredPosition = scaledPos;
        }
        else
        {
            playerIconRect.anchoredPosition = scaledPos;
            miniMapRect.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        if (playerObj == null || playerIconRect == null || miniMapRect == null) return;

        UpdatePlayerIconRotation();
        UpdateMinimapImageRotation();

        Vector2 scaledPos = GetScaledPlayerPos();
        Vector2 scaledMapPos = GetScaledMapPos();

        isOverDeadZone = IsBeyondDeadZone(scaledPos);

        // 目标位置
        Vector2 targetMapPosA = scaledMapPos * MapOffsetFactor; // 等价于 -scaledPos / 2.0f
        Vector2 targetMapPosB = Vector2.zero;

        // 插值
        Vector2 lerpPosMapA = Vector2.Lerp(miniMapRect.anchoredPosition, scaledMapPos, Time.deltaTime * lerpSpeed);
        Vector2 lerpPosMapB = Vector2.Lerp(miniMapRect.anchoredPosition, targetMapPosB, Time.deltaTime * lerpSpeed);

        // 应用
        playerIconRect.anchoredPosition = scaledPos;
        miniMapRect.anchoredPosition = isOverDeadZone ? lerpPosMapA : lerpPosMapB;
    }

    private bool ValidateReferences()
    {
        if (miniMapImage == null)
        {
            Debug.LogError("MiniMapTest: miniMapImage 未设置。");
            return false;
        }
        if (minimapIconPivot == null)
        {
            Debug.LogError("MiniMapTest: minimapIconPivot 未设置。");
            return false;
        }
        if (playerArmatureIcon == null)
        {
            Debug.LogError("MiniMapTest: playerArmatureIcon 未设置。");
            return false;
        }

        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("MiniMapTest: 未找到带有 Player 标签的对象。");
            return false;
        }

        if (buttonZoomIn == null || buttonZoomOut == null)
        {
            Debug.LogError("MiniMapTest: Zoom buttons 未设置。");
        }
        else
        {
            //移除已经注册的事件，防止重复注册
            buttonZoomIn.onClick.RemoveAllListeners();
            buttonZoomOut.onClick.RemoveAllListeners();
        }
        return true;
    }

    private void InitializePlayerIcon()
    {
        playerIcon = Instantiate(playerArmatureIcon);
        playerIcon.transform.SetParent(minimapIconPivot.transform, false);
        playerIcon.transform.localScale = Vector3.one;
    }

    private void CacheRectTransforms()
    {
        miniMapRect = miniMapImage.GetComponent<RectTransform>();
        playerIconRect = playerIcon.GetComponent<RectTransform>();
    }

    private Vector2 GetScaledPlayerPos()
    {
        float zoomFactor = ((int)zoomLevel) / 100f;
        Vector2 deltaPos = (playerObj.transform.position - originPos).ToVector2();
        return new Vector2(deltaPos.x * scaleRateX * currentCamAspect * zoomFactor, deltaPos.y * scaleRateY * zoomFactor);
    }

    private Vector2 GetScaledMapPos()
    {
        Vector3 worldForward = Vector3.forward;
        Vector3 cameraForward = Camera.main.transform.forward;

        float angleY = Vector2.SignedAngle(
            new Vector2(worldForward.x, worldForward.z),
            new Vector2(cameraForward.x, cameraForward.z)
        );

        float zoomFactor = ((int)zoomLevel) / 100f;
        Vector2 deltaPos = (playerObj.transform.position - originPos).ToVector2();
        Vector2 scaledPos = new Vector2(deltaPos.x * scaleRateX * currentCamAspect * zoomFactor, deltaPos.y * scaleRateY * zoomFactor);

        // 将 mapPos 旋转 angleY（角度制）
        float rad = angleY * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        scaledPos = new Vector2(
            scaledPos.x * cos - scaledPos.y * sin,
            scaledPos.x * sin + scaledPos.y * cos
        );
        return -scaledPos;
    }

    private bool IsBeyondDeadZone(Vector2 scaledPos)
    {
        float zoomFactor = ((int)zoomLevel) / 100f;
        return Mathf.Abs(scaledPos.x) > deadZoneSize * zoomFactor || Mathf.Abs(scaledPos.y) > deadZoneSize * zoomFactor;
    }

    private void UpdatePlayerIconRotation()
    {
        Vector3 worldForward = Vector3.forward;
        Vector3 cameraForward = Camera.main.transform.forward;
        float angleY = Vector2.SignedAngle(
            new Vector2(worldForward.x, worldForward.z),
            new Vector2(cameraForward.x, cameraForward.z)
        );

        Vector3 playerForwardRotation = playerObj.transform.forward;
        float playerYRotation = Vector2.SignedAngle(
            new Vector2(worldForward.x, worldForward.z),
            new Vector2(playerForwardRotation.x, playerForwardRotation.z)
        );
        playerIconRect.rotation = Quaternion.Euler(0f, 0f, (angleY + playerYRotation));
    }

    private void UpdateMinimapImageRotation()
    {
        Vector3 worldForward = Vector3.forward;
        Vector3 cameraForward = Camera.main.transform.forward;

        float angleY = Vector2.SignedAngle(
            new Vector2(worldForward.x, worldForward.z),
            new Vector2(cameraForward.x, cameraForward.z)
        );
        miniMapRect.transform.rotation = Quaternion.Euler(0f, 0f, angleY);  
    }

    private void OnDrawGizmos()
    {
        // 绘制 originPos（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(originPos, 1.0f);

        // 如果 playerObj 存在，绘制其位置与向量 v
        if (playerObj != null)
        {
            Vector3 playerPos = playerObj.transform.position;

            // 绘制 player 位置（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerPos, 1.0f);

            // 绘制向量 v = playerPos - originPos（黄色，从 originPos 发射）
            Vector3 v = playerPos - originPos;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(originPos, v);
            // 如更偏好直线，也可使用：
            // Gizmos.DrawLine(originPos, playerPos);
        }
    }
}
