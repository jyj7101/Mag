using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtil;

public class GameManager : MonoBehaviour
{
    private bool _cursorLock;
    void Start()
    {
        _cursorLock = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            _cursorLock = Util.TrueFalseTranslater(_cursorLock);

        LockCursor();
    }

    private void LockCursor()
    {
        if (_cursorLock)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
