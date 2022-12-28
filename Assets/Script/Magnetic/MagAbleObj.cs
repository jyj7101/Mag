using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagAbleObj : MonoBehaviour
{
    [SerializeField] MagStatus magStatus;
    public MagStatus.MagState magState;

    public Renderer _renderer;

    private void Start()
    {
        magState = MagStatus.MagState.Middle; // �߸�
        _renderer = GetComponent<Renderer>();
    }
 
    private void Update()
    {
        ChangeColor();
    }
    /*
     1. �� ������Ʈ�� ���� �˾Ƴ���
     2. �Ÿ� �缭 �η��̵� ô���̵� �ֱ�

     */


    // �Ʒ� �� �Լ� ������...?
    [Tooltip("ô��")]
    private void Repulsioin()
    {

    }
    [Tooltip("�η�")]
    private void Attraction()
    {

    }

    private void ChangeColor()
    {
        if(magState == MagStatus.MagState.Middle)
        {
            _renderer.material.color = Color.black;
        }
        else if(magState == MagStatus.MagState.N)
        {
            _renderer.material.color = Color.red;
        }
        else if(magState == MagStatus.MagState.S)
        {
            _renderer.material.color = Color.blue;
        }
    }

}
