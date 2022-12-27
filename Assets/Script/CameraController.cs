using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _turnSpeed = 4.0f;
    [SerializeField] private float _moveSpeed = 1f;
    private float _xRotate = 0.0f;

    private RaycastHit _rayHit;
    private float _rayMaxDis = 30;



    private void Start()
    {
        
    }

    private void Update()
    {
        Rotate();
        Move();
        ShootMag();
        Debug.DrawRay(transform.position, transform.forward * _rayMaxDis, Color.blue, 0.3f);
    }

    private void Rotate()
    {
        float yRotateSize = Input.GetAxis("Mouse X") * _turnSpeed;
        float yRotate = transform.eulerAngles.y + yRotateSize;

        float xRotateSize = -Input.GetAxis("Mouse Y") * _turnSpeed;
        _xRotate = Mathf.Clamp(_xRotate + xRotateSize, -45, 45);

        transform.eulerAngles = new Vector3(_xRotate, yRotate, 0);
    }

    private void Move()
    {
        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.Translate(dir * _moveSpeed * Time.deltaTime);
    }


    private void ShootMag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("left click");
            Physics.Raycast(transform.position, transform.forward, out _rayHit, _rayMaxDis);
            if (_rayHit.transform.gameObject.CompareTag("MagnatableObject"))
            {
                _rayHit.transform.gameObject.GetComponent<MagAbleObj>().magState = MagStatus.MagState.N;
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("right click");
            Physics.Raycast(transform.position, transform.forward, out _rayHit, _rayMaxDis);
            if (_rayHit.transform.gameObject.CompareTag("MagnatableObject"))
            {
                _rayHit.transform.gameObject.GetComponent<MagAbleObj>().magState = MagStatus.MagState.S;
            }
        }
    }
}
