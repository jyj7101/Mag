using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrayScale : MonoBehaviour
{

    public bool grayOn;

    void Update()
    {
        float gray;
        if (grayOn)
            gray = 1;
        else
            gray = 0;
        Shader.SetGlobalFloat("_GrayOn", gray);
    }

}
