using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Tooltip("�÷��̾� �̵� �ӵ�")]
    public float moveSpeed = 3;
    [Tooltip("�÷��̾� ȸ�� �ӵ�")] // ���콺 ������ ���� �ٲ�����
    public float turnSpeed = 3;

    public int leftClick = 0;
    public int rightClick = 1;

    public float _rayMaxDis = 30;

    /*
        zoomingTime : 
    �� �ƿ� �� ���� ������µ� 
    �� �� �ڷ�ƾ�� while���� �ƴ϶�
    �Ҽ����� ���ԵǸ� ��Ȯ�� ����� �ȵ� �� ����
     
     */
    public float zoomingTime = 1;
}
