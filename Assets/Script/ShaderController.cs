using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderController : MonoBehaviour
{
    public float height;
    
    #region PRIVATE
    private Material mat;
    private const string ShaderName = "_Height";
    #endregion
    
    private void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        mat.SetFloat(Shader.PropertyToID(ShaderName), height);
        if (Input.GetKey(KeyCode.E))
            height++;
        if (Input.GetKey(KeyCode.Q))
            height--;
    }
}
