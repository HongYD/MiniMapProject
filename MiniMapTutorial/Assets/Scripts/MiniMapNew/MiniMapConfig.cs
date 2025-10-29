using System;
using UnityEngine;

[Serializable]
public class MiniMapConfig
{
    public Vector3 worldTopLeft;
    public Vector3 worldBottomLeft;
    public float ppmX;     // X��������/�ף���ÿ�׶�Ӧ�������صĵ�����������Ķ��壩
    public float ppmY;     // Y��������/��
    public int capSize;    // ͼƬ�ߴ�
    public float aspect;   // ����ӿڿ�߱�
}