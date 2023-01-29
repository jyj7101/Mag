using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dice : MonoBehaviour
{
    public Texture[] DiceTextures;

    private MeshRenderer meshRenderer;

    public int diceNum;

    public float colorN;
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = DiceTextures[diceNum];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(Prevention());
        }
    }
    
    void DownDiceNum()
    {
        diceNum--;
        meshRenderer.material.mainTexture = DiceTextures[diceNum];
    }

    IEnumerator Prevention()
    {
        meshRenderer.material.mainTexture = DiceTextures[6];
        while (colorN < 1)
        {
            meshRenderer.material.color = new Color(1, colorN, colorN, 1);
            colorN += Time.deltaTime;
            yield return null;
        }
        DownDiceNum();
        colorN = 0;
        meshRenderer.material.color = new Color(1, 1, 1, 1);
    }
}