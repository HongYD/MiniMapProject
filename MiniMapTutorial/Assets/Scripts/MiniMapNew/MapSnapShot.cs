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
            Debug.LogError("MapSnapShot: snapShotCam δ���á�");
            return;
        }

        // �������ǰ��
        if (!snapShotCam.orthographic)
        {
            Debug.LogWarning("MapSnapShot: ���齫 snapShotCam ��Ϊ���������orthographic = true����");
        }

        // ͳһʹ�� nearClipPlane ��Ϊ depth��������⣨������ XY ӳ���� z �޹أ�
        float depth = Mathf.Max(snapShotCam.nearClipPlane, 0.0f);
        
        /*
         *(0,1)|������������������������|(1,1)
         *     |            |
         *     |            |
         *     |            |
         *(0,0)|------------|(1,0)            
        */
        //�ӿ�����ϵ�У����Ͻǡ����½ǡ����Ͻǵ�����
        viewportPivotTopLeft = new Vector3(0f, 1f, depth);
        viewportPivotButtomLeft = new Vector3(0f, 0f, depth);
        viewportPivotTopRight = new Vector3(1f, 1f, depth);

        // �����ӿ�����ϵ�У����Ͻǡ����½ǡ����Ͻǵ���������
        worldPivotTopLeft = snapShotCam.ViewportToWorldPoint(viewportPivotTopLeft);
        worldPivotButtomLeft = snapShotCam.ViewportToWorldPoint(viewportPivotButtomLeft);
        viewportPivotTopRight = snapShotCam.ViewportToWorldPoint(viewportPivotTopRight);

        // ��������ϵ�¸߶�����
        float worldHeight = Vector3.Distance(worldPivotTopLeft, worldPivotButtomLeft);
        float worldWidth = Vector3.Distance(worldPivotTopLeft, viewportPivotTopRight);

        Debug.Log($"��������ϵ���Ͻ�: {worldPivotTopLeft}");
        Debug.Log($"��������ϵ���½�: {worldPivotButtomLeft}");
        Debug.Log($"��������ϵ���Ͻ�: {viewportPivotTopRight}");
        Debug.Log($"����ɼ����: {worldWidth}���߶�: {worldHeight}�����������");
        Debug.Log($"��ǰ����Ŀ�߱�: {snapShotCam.aspect}");

        //����ߴ�������أ��õ���/���أ�Ҳ����˵����������ϵ��ÿǰ��һ�ף���ͼ���ƶ���������
        float ppmY = capSize / Mathf.Max(worldHeight, Mathf.Epsilon);
        float ppmX = capSize / Mathf.Max(worldWidth, Mathf.Epsilon);
        Debug.Log($"��/���أ�X����={ppmX}��Y����={ppmY}��(Ҳ����˵����������ϵ��ÿǰ��һ�ף���ͼ���ƶ���������)");

        // ������ֲ�ı���·��
        volume = System.Environment.CurrentDirectory;
        mapPath = Path.Combine(volume, "PMSTemp", "MapPath");
        await TakeASnapShot();

        //����֮�󣬽����ղ���д���ļ���mapConfig.json
        //mapConfig�����ݰ�������������ϵ���Ͻǡ����½����꣬X�����Y�������/����(ppmX,ppmY)��ͼƬ�ߴ�(capSize)
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
                Object.Destroy(tex);
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
}
