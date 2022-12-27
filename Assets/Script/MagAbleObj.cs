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
        magState = MagStatus.MagState.Middle; // 중립
        _renderer = GetComponent<Renderer>();
    }
 
    private void Update()
    {
        ChangeColor();
    }
    /*
     1. 두 오브젝트의 상태 알아내기
     2. 거리 재서 인력이든 척력이든 넣기

     */


    // 아래 두 함수 구현이...?
    [Tooltip("척력")]
    private void Repulsioin()
    {

    }
    [Tooltip("인력")]
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
