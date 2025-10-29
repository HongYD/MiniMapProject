using Cysharp.Threading.Tasks;
using System;
using System.IO;
using UnityEngine;

public class MapSnapShot : MonoBehaviour
{
    public Camera snapShotCam;
    public string mapPath;
    public int capSize = 1024;
    private string volume;

    private Vector3 viewportPivotTopLeft;
    private Vector3 viewportPivotButtomLeft;
    private Vector3 viewportPivotTopRight;

    private Vector3 worldPivotTopLeft;
    private Vector3 worldPivotButtomLeft;

    // 新增：记录 ppm 以便写入配置
    private float ppmX;
    private float ppmY;

    async void Start()
    {
        if (snapShotCam == null)
        {
            Debug.LogError("MapSnapShot: snapShotCam 未设置。");
            return;
        }

        if (!snapShotCam.orthographic)
        {
            Debug.LogWarning("MapSnapShot: 建议将 snapShotCam 设为正交相机（orthographic = true）。");
        }

        float depth = Mathf.Max(snapShotCam.nearClipPlane, 0.0f);

        viewportPivotTopLeft = new Vector3(0f, 1f, depth);
        viewportPivotButtomLeft = new Vector3(0f, 0f, depth);
        viewportPivotTopRight = new Vector3(1f, 1f, depth);

        // 计算视口坐标系到世界坐标
        worldPivotTopLeft = snapShotCam.ViewportToWorldPoint(viewportPivotTopLeft);
        worldPivotButtomLeft = snapShotCam.ViewportToWorldPoint(viewportPivotButtomLeft);
        viewportPivotTopRight = snapShotCam.ViewportToWorldPoint(viewportPivotTopRight);

        float worldHeight = Vector3.Distance(worldPivotTopLeft, worldPivotButtomLeft);
        float worldWidth = Vector3.Distance(worldPivotTopLeft, viewportPivotTopRight);

        Debug.Log($"世界坐标系左上角: {worldPivotTopLeft}");
        Debug.Log($"世界坐标系左下角: {worldPivotButtomLeft}");
        Debug.Log($"世界坐标系右上角: {viewportPivotTopRight}");
        Debug.Log($"世界可见宽度: {worldWidth}，高度: {worldHeight}（正交相机）");
        Debug.Log($"当前相机的宽高比: {snapShotCam.aspect}");

        // 米/像素（或像素/米，与你使用处保持一致）
        ppmY = capSize / Mathf.Max(worldHeight, Mathf.Epsilon);
        ppmX = capSize / Mathf.Max(worldWidth, Mathf.Epsilon);
        Debug.Log($"米/像素：X方向={ppmX}，Y方向={ppmY}。(也就是说再世界坐标系下每前进一米，贴图上移动多少像素)");

        // 保存目录
        volume = System.Environment.CurrentDirectory;
        mapPath = Path.Combine(volume, "PMSTemp", "MapPath");

        // 截图
        await TakeASnapShot();

        // 截图完成后，保存配置
        SaveMapConfig();
    }

    private async UniTask TakeASnapShot()
    {
        // 等待到帧末以确保摄像机状态与场景渲染稳定
        await UniTask.WaitForEndOfFrame(this);

        // 使用临时 RT，禁止动态分辨率以获得精确尺寸
        var rt = RenderTexture.GetTemporary(capSize, capSize, 24, RenderTextureFormat.ARGB32);
        rt.useDynamicScale = false;

        // 备份状态
        var prevActive = RenderTexture.active;
        var prevTarget = snapShotCam.targetTexture;

        try
        {
            snapShotCam.targetTexture = rt;

            // 显式渲染一帧
            snapShotCam.Render();

            // 读取像素
            RenderTexture.active = rt;
            var tex = new Texture2D(capSize, capSize, TextureFormat.RGBA32, false);
            try
            {
                tex.ReadPixels(new Rect(0, 0, capSize, capSize), 0, 0);
                tex.Apply(false, false);

                SaveTexture(tex);
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }
        finally
        {
            // 还原状态并释放
            RenderTexture.active = prevActive;
            snapShotCam.targetTexture = prevTarget;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private void SaveTexture(Texture2D targetTexture)
    {
        try
        {
            if (!Directory.Exists(mapPath))
            {
                Directory.CreateDirectory(mapPath);
            }

            string savePath = Path.Combine(mapPath, "map.png");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            var png = targetTexture.EncodeToPNG();
            File.WriteAllBytes(savePath, png);
            Debug.Log($"地图截图已保存: {savePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"保存地图截图失败: {ex.Message}");
        }
    }

    // 新增：保存截图参数为 json（与 map.png 同目录）
    private void SaveMapConfig()
    {
        try
        {
            if (!Directory.Exists(mapPath))
            {
                Directory.CreateDirectory(mapPath);
            }

            var cfg = new MiniMapConfig
            {
                worldTopLeft = worldPivotTopLeft,
                worldBottomLeft = worldPivotButtomLeft,
                ppmX = ppmX,
                ppmY = ppmY,
                capSize = capSize,
                aspect = snapShotCam != null ? snapShotCam.aspect : 1f
            };

            string json = JsonUtility.ToJson(cfg, true); // pretty-print
            string configPath = Path.Combine(mapPath, "mapConfig.json");
            File.WriteAllText(configPath, json);
            Debug.Log($"地图配置已保存: {configPath}\n{json}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存地图配置失败: {ex.Message}");
        }
    }
}
