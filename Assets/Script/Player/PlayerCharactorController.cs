using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharactorController : MonoBehaviour
{
    private Camera mainCamera;
    private PlayerInfo playerInfo;

    private float _xRotate = 0; // 플레이어 회전에 관한

    private RaycastHit _rayHit;

    private IEnumerator zoomCoroutine;
    
    private void Start()
    {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        playerInfo = GetComponent<PlayerInfo>();
        zoomCoroutine = ZoomOut(); // 코루틴 초기화 / 카메라의 filed of view를 inspector에서 수정이 불가능하게 됨
    }
    
    void Update()
    {
        {
            PlayerMove();
            PlayerRot();
        }

        {
            ShootMag();
            ZoomCamera();
        }
        Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * playerInfo._rayMaxDis, Color.blue, 0.3f);

    }

    void PlayerMove()
    {
        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.Translate(dir * playerInfo.moveSpeed * Time.deltaTime);
    }

    void PlayerRot()
    {
        float yRotateSize = Input.GetAxis("Mouse X") * playerInfo.turnSpeed;
        float yRotate = transform.eulerAngles.y + yRotateSize;

        transform.eulerAngles = new Vector3(0, yRotate, 0);

        float xRotateSize = -Input.GetAxis("Mouse Y") * playerInfo.turnSpeed;
        _xRotate = Mathf.Clamp(_xRotate + xRotateSize, -45, 45);

        mainCamera.transform.eulerAngles = new Vector3(_xRotate, yRotate, 0);
    }


    private void ShootMag()
    {
        if (Input.GetMouseButtonDown(playerInfo.leftClick))
        {
            Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out _rayHit, playerInfo._rayMaxDis);

            // 예외처리
            if(_rayHit.transform == null || !_rayHit.transform.gameObject.CompareTag("MagnatableObject"))
            {
                return;
            }
            else if (_rayHit.transform.gameObject.CompareTag("MagnatableObject"))
            {
                switch (_rayHit.transform.gameObject.GetComponent<MagAbleObj>().magState)
                {
                    case MagStatus.MagState.Middle :
                        _rayHit.transform.gameObject.GetComponent<MagAbleObj>().magState = MagStatus.MagState.N;
                        break;
                    case MagStatus.MagState.N:
                        _rayHit.transform.gameObject.GetComponent<MagAbleObj>().magState = MagStatus.MagState.Middle;
                        break;
                    case MagStatus.MagState.S:
                        _rayHit.transform.gameObject.GetComponent<MagAbleObj>().magState = MagStatus.MagState.N;
                        break;
                }
            }
        }
    }

    void ZoomCamera()
    {
        //field of view 30 - 60
        if (Input.GetMouseButton(playerInfo.rightClick))
        {

            StopCoroutine(zoomCoroutine);
            zoomCoroutine = ZoomIn();
            StartCoroutine(zoomCoroutine);
        }
        else if (Input.GetMouseButtonUp(playerInfo.rightClick))
        {
            zoomCoroutine = ZoomOut();
            StartCoroutine(zoomCoroutine);
        }
    }

    IEnumerator ZoomIn()
    {
        if (mainCamera.fieldOfView > 30)
        {
            mainCamera.fieldOfView -= playerInfo.zoomingTime;
            yield return null;
        }
    }

    IEnumerator ZoomOut()
    {
        while (mainCamera.fieldOfView <= 60)
        {
            mainCamera.fieldOfView += playerInfo.zoomingTime;
            yield return null;
        }
        mainCamera.fieldOfView = 60;
    }
}
