using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GrayScale : MonoBehaviour
{
    [Range(0, 1)]
    public float grayScaleAmount;

    [SerializeField]
    private Material grayScale;
    private void Update()
    {
        grayScale.SetFloat("_GrayScale", grayScaleAmount);
        //Shader.SetGlobalFloat("GrayScale", grayScaleAmount);
    }
}
