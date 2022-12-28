using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Tooltip("플레이어 이동 속도")]
    public float moveSpeed = 3;
    [Tooltip("플레이어 회전 속도")] // 마우스 감도에 따라 바뀌어야함
    public float turnSpeed = 3;

    public int leftClick = 0;
    public int rightClick = 1;

    public float _rayMaxDis = 30;

    /*
        zoomingTime : 
    줌 아웃 할 때는 상관없는데 
    줌 인 코루틴이 while문이 아니라서
    소수점이 들어가게되면 정확한 계산이 안될 수 있음
     
     */
    public float zoomingTime = 1;
}
