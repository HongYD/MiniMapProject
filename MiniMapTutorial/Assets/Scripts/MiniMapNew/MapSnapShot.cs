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

    // ��������¼ ppm �Ա�д������
    private float ppmX;
    private float ppmY;

    async void Start()
    {
        if (snapShotCam == null)
        {
            Debug.LogError("MapSnapShot: snapShotCam δ���á�");
            return;
        }

        if (!snapShotCam.orthographic)
        {
            Debug.LogWarning("MapSnapShot: ���齫 snapShotCam ��Ϊ���������orthographic = true����");
        }

        float depth = Mathf.Max(snapShotCam.nearClipPlane, 0.0f);

        viewportPivotTopLeft = new Vector3(0f, 1f, depth);
        viewportPivotButtomLeft = new Vector3(0f, 0f, depth);
        viewportPivotTopRight = new Vector3(1f, 1f, depth);

        // �����ӿ�����ϵ����������
        worldPivotTopLeft = snapShotCam.ViewportToWorldPoint(viewportPivotTopLeft);
        worldPivotButtomLeft = snapShotCam.ViewportToWorldPoint(viewportPivotButtomLeft);
        viewportPivotTopRight = snapShotCam.ViewportToWorldPoint(viewportPivotTopRight);

        float worldHeight = Vector3.Distance(worldPivotTopLeft, worldPivotButtomLeft);
        float worldWidth = Vector3.Distance(worldPivotTopLeft, viewportPivotTopRight);

        Debug.Log($"��������ϵ���Ͻ�: {worldPivotTopLeft}");
        Debug.Log($"��������ϵ���½�: {worldPivotButtomLeft}");
        Debug.Log($"��������ϵ���Ͻ�: {viewportPivotTopRight}");
        Debug.Log($"����ɼ����: {worldWidth}���߶�: {worldHeight}�����������");
        Debug.Log($"��ǰ����Ŀ�߱�: {snapShotCam.aspect}");

        // ��/���أ�������/�ף�����ʹ�ô�����һ�£�
        ppmY = capSize / Mathf.Max(worldHeight, Mathf.Epsilon);
        ppmX = capSize / Mathf.Max(worldWidth, Mathf.Epsilon);
        Debug.Log($"��/���أ�X����={ppmX}��Y����={ppmY}��(Ҳ����˵����������ϵ��ÿǰ��һ�ף���ͼ���ƶ���������)");

        // ����Ŀ¼
        volume = System.Environment.CurrentDirectory;
        mapPath = Path.Combine(volume, "PMSTemp", "MapPath");

        // ��ͼ
        await TakeASnapShot();

        // ��ͼ��ɺ󣬱�������
        SaveMapConfig();
    }

    private async UniTask TakeASnapShot()
    {
        // �ȴ���֡ĩ��ȷ�������״̬�볡����Ⱦ�ȶ�
        await UniTask.WaitForEndOfFrame(this);

        // ʹ����ʱ RT����ֹ��̬�ֱ����Ի�þ�ȷ�ߴ�
        var rt = RenderTexture.GetTemporary(capSize, capSize, 24, RenderTextureFormat.ARGB32);
        rt.useDynamicScale = false;

        // ����״̬
        var prevActive = RenderTexture.active;
        var prevTarget = snapShotCam.targetTexture;

        try
        {
            snapShotCam.targetTexture = rt;

            // ��ʽ��Ⱦһ֡
            snapShotCam.Render();

            // ��ȡ����
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
            // ��ԭ״̬���ͷ�
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
            Debug.Log($"��ͼ��ͼ�ѱ���: {savePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"�����ͼ��ͼʧ��: {ex.Message}");
        }
    }

    // �����������ͼ����Ϊ json���� map.png ͬĿ¼��
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
            Debug.Log($"��ͼ�����ѱ���: {configPath}\n{json}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"�����ͼ����ʧ��: {ex.Message}");
        }
    }
}
