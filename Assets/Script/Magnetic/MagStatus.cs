using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "Magnatable", menuName = "ScriptableObject/MagnatableObjData", order = int.MaxValue)]
public class MagStatus : ScriptableObject
{
    public enum MagState
    {
        NONE,
        Middle,
        N,
        S
    }

}
