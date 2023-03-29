using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrayScale : MonoBehaviour
{

    public Color ChangeColor;

    void Update()
    {
        Shader.SetGlobalColor("_ChangeColor", ChangeColor);
    }

}
