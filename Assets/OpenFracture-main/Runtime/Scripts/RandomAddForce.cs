using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomAddForce : MonoBehaviour
{
    public float xForce = 500;
    public float yForce = 500;
    public float zForce = 500;

    IEnumerator Start()
    {
        Rigidbody rigi = GetComponent<Rigidbody>();
        rigi.AddForce(new Vector3(Random.Range(-xForce, xForce), Random.Range(-yForce, yForce),Random.Range(-zForce, zForce)));
        
        yield return new WaitForSeconds(3f);
        Material m = GetComponent<MeshRenderer>().sharedMaterial;
        m.SetFloat("Dissolve", 1);
        float runTime = 0;
        while (runTime < 1)
        {
            m.SetFloat("Dissolve", runTime);
            yield return null;
            Debug.Log("need destroy");
            runTime += 0.1f;
        }

        runTime = 1;

    }
}
