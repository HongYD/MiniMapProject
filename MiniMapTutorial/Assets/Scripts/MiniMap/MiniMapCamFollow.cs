using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMapCamFollow : MonoBehaviour
{
    private GameObject followTarget;
    private float hightOffset = 5.0f;


    private void Start()
    {
        followTarget = GameObject.Find("PlayerArmature");
    }

    private void Update()
    {
        if (followTarget != null)
        {
            Transform follow = followTarget.transform;
            this.transform.position = new Vector3(follow.position.x, followTarget.transform.position.y + hightOffset, follow.position.z);
        }
        else
        {
            followTarget = GameObject.Find("PlayerArmature");
        }
    }
}
