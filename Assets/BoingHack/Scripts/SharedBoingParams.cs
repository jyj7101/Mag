using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace BoingHack
{
    //scriptableObject

    [CreateAssetMenu(fileName = "BoingHackParams", menuName = "Boing Hack/Shared Boing Params", order = 550)]
    public class SharedBoingParams : ScriptableObject
    {
        public Params Params;

        public SharedBoingParams()
        {
            Params.Init();
        }
    }

}