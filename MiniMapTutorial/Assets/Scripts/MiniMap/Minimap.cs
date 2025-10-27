using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [SerializeField]
    private GameObject minimapIconPivot;//Image
    [SerializeField]
    private RawImage minimapRawImage;//�����Ⱦ��Raw Image

    [SerializeField]
    private Button buttonUp;
    [SerializeField]
    private Button buttonDown;

    private Camera miniMapCamera;//С��ͼ���

    GameObject playerIcon;
    Transform playerTrans; //��������ϵ�µ�����Transform

    Vector3 centerPosMiniMap; //С��ͼ���ĵ�����

    Vector2 xSizeUI; //����ӿ�����x
    Vector2 ySizeUI; //����ӿ�����y

    float cameraViewHalfSize;// ����ӿڴ�С��һ��

    float mainCamRotY;//�������תŷ���ǵ�Y����

    private List<MinimapObj> minimapObjs;

    private void Start()
    {
        //��ʼ��
        minimapObjs = new List<MinimapObj>();
        miniMapCamera = GameObject.Find("MiniMapCam").gameObject.GetComponent<Camera>();
        playerTrans = GameObject.Find("PlayerArmature").gameObject.transform;

        //������ͼ�����õ�С��ͼUI�����ĵ㣬�����丸�����õ�minimapIconPivot
        playerIcon = Instantiate(Resources.Load<GameObject>("Prefabs/PlayerIcon"));
        playerIcon.transform.SetParent(minimapIconPivot.transform, false);
        playerIcon.GetComponent<RectTransform>().localPosition = centerPosMiniMap;


        //С��ͼ��������
        float posX = minimapIconPivot.GetComponent<RectTransform>().anchoredPosition.x;
        float posY = minimapIconPivot.GetComponent<RectTransform>().anchoredPosition.y;
        centerPosMiniMap = new Vector2(posX, posY);

        //С��ͼUI��������ֵ����Сֵ
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


        //1.��ȡ����ͼ����ת��Y
        Vector3 cameraLookAt = Camera.main.transform.forward;//������䷽��
        Vector3 playerLocalForward = playerTrans.transform.forward;//��������
        float angleY = Vector2.SignedAngle(new Vector2(cameraLookAt.x, cameraLookAt.z), new Vector2(playerLocalForward.x, playerLocalForward.z));//��������������������н�
        playerIcon.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleY);//������ͼ����ת����Ƕ�

        Vector3 viewportPlayerPos = miniMapCamera.worldToCameraMatrix.MultiplyPoint(playerTrans.position);//������С��ͼ����ӿ�����ϵ�µ�����

        //2.����ÿ��ע��ĵ�ͼ��Ϣ������������ת��λ�ã�����С��ͼ������ӿ�����ϵ�£�����ӿ�����ϵ������ӳ�䵽UI����ϵ��
        for (int i=0;i< minimapObjs.Count; i++)
        {
            Vector3 viewportPos = miniMapCamera.worldToCameraMatrix.MultiplyPoint(minimapObjs[i].owner.transform.position);
            //����������˵�λ��
            Vector3 relativePosViewport = viewportPos - viewportPlayerPos;
            float distToPlayer = Vector3.Distance(viewportPlayerPos, viewportPos);
            float deltaY = Camera.main.transform.eulerAngles.y - Mathf.Atan2(relativePosViewport.x, relativePosViewport.y) * Mathf.Rad2Deg + 90;

            relativePosViewport.x = distToPlayer * Mathf.Cos(deltaY * Mathf.Deg2Rad);//���Ǻ���
            relativePosViewport.y = distToPlayer * Mathf.Sin(deltaY * Mathf.Deg2Rad);
            float rateX = Mathf.Clamp01((relativePosViewport.x - (-cameraViewHalfSize)) / (cameraViewHalfSize * 2));
            float xUIPos = xSizeUI.x + (xSizeUI.y - xSizeUI.x) * rateX;
            float rateY = Mathf.Clamp01((relativePosViewport.y - (-cameraViewHalfSize)) / (cameraViewHalfSize * 2));
            float yUIPos = ySizeUI.x + (ySizeUI.y - ySizeUI.x) * rateY;
            Vector2 miniMapPos = new Vector2(xUIPos, yUIPos);//���UI����
            minimapObjs[i].icon.transform.SetParent(minimapIconPivot.transform);//���ø���
            minimapObjs[i].icon.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
            minimapObjs[i].icon.gameObject.GetComponent<RectTransform>().localPosition = miniMapPos;//��ֵ
        }

        if (Mathf.Abs(mainCamRotY - Camera.main.transform.eulerAngles.y) > 1.0f)
        {
            minimapRawImage.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, mainCamRotY);
            mainCamRotY = Camera.main.transform.eulerAngles.y;
        }
    }


    private void RegisterMinimapObjects()
    {
        //ע�����е�ͼͼ��
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
