using Cysharp.Threading.Tasks;
using System.IO;
using UnityEngine;

public class MapSnapShot : MonoBehaviour
{
    public Camera snapShotCam;
    public string mapPath;
    public int capSize = 1024;
    private string volume;

    //private Vector3 screenPivotTopLeft;
    //private Vector3 screenPivotButtomLeft;

    private Vector3 viewportPivotTopLeft;
    private Vector3 viewportPivotButtomLeft;

    private Vector3 viewportPivotTopRight;

    private Vector3 worldPivotTopLeft;
    private Vector3 worldPivotButtomLeft;

    async void Start()
    {
        if (snapShotCam == null)
        {
            Debug.LogError("MapSnapShot: snapShotCam 未设置。");
            return;
        }

        // 正交相机前提
        if (!snapShotCam.orthographic)
        {
            Debug.LogWarning("MapSnapShot: 建议将 snapShotCam 设为正交相机（orthographic = true）。");
        }

        // 统一使用 nearClipPlane 作为 depth，便于理解（正交下 XY 映射与 z 无关）
        float depth = Mathf.Max(snapShotCam.nearClipPlane, 0.0f);
        
        /*
         *(0,1)|————————————|(1,1)
         *     |            |
         *     |            |
         *     |            |
         *(0,0)|------------|(1,0)            
        */
        //视口坐标系中，左上角、左下角、右上角的坐标
        viewportPivotTopLeft = new Vector3(0f, 1f, depth);
        viewportPivotButtomLeft = new Vector3(0f, 0f, depth);
        viewportPivotTopRight = new Vector3(1f, 1f, depth);

        // 计算视口坐标系中，左上角、左下角、右上角的世界坐标
        worldPivotTopLeft = snapShotCam.ViewportToWorldPoint(viewportPivotTopLeft);
        worldPivotButtomLeft = snapShotCam.ViewportToWorldPoint(viewportPivotButtomLeft);
        viewportPivotTopRight = snapShotCam.ViewportToWorldPoint(viewportPivotTopRight);

        // 世界坐标系下高度与宽度
        float worldHeight = Vector3.Distance(worldPivotTopLeft, worldPivotButtomLeft);
        float worldWidth = Vector3.Distance(worldPivotTopLeft, viewportPivotTopRight);

        Debug.Log($"世界坐标系左上角: {worldPivotTopLeft}");
        Debug.Log($"世界坐标系左下角: {worldPivotButtomLeft}");
        Debug.Log($"世界坐标系右上角: {viewportPivotTopRight}");
        Debug.Log($"世界可见宽度: {worldWidth}，高度: {worldHeight}（正交相机）");
        Debug.Log($"当前相机的宽高比: {snapShotCam.aspect}");

        //世界尺寸除以像素，得到米/像素，也就是说在世界坐标系下每前进一米，贴图上移动多少像素
        float ppmY = capSize / Mathf.Max(worldHeight, Mathf.Epsilon);
        float ppmX = capSize / Mathf.Max(worldWidth, Mathf.Epsilon);
        Debug.Log($"米/像素：X方向={ppmX}，Y方向={ppmY}。(也就是说再世界坐标系下每前进一米，贴图上移动多少像素)");

        // 更可移植的保存路径
        volume = System.Environment.CurrentDirectory;
        mapPath = Path.Combine(volume, "PMSTemp", "MapPath");
        await TakeASnapShot();

        //快照之后，将快照参数写入文件，mapConfig.json
        //mapConfig的内容包括：世界坐标系左上角、左下角坐标，X方向和Y方向的米/像素(ppmX,ppmY)，图片尺寸(capSize)
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
                Object.Destroy(tex);
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
}
