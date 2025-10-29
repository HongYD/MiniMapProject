using System;
using UnityEngine;

[Serializable]
public class MiniMapConfig
{
    public Vector3 worldTopLeft;
    public Vector3 worldBottomLeft;
    public float ppmX;     // X方向：像素/米（或每米对应多少像素的倒数，根据你的定义）
    public float ppmY;     // Y方向：像素/米
    public int capSize;    // 图片尺寸
    public float aspect;   // 相机视口宽高比
}