using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractureChildren : MonoBehaviour
{
    private static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");
    public float dissolveTime = 10000;
    public float xForce = 100;
    public float yForce = 100;
    public float zForce = 100;

    IEnumerator Start()
    {
        Rigidbody rigi = GetComponent<Rigidbody>();

        rigi.AddForce(new Vector3(Random.Range(-xForce, xForce), Random.Range(-yForce, yForce),Random.Range(-zForce, zForce)));
    
        
        yield return new WaitForSeconds(3f);
        Material m = GetComponent<MeshRenderer>().sharedMaterial;
        m.SetFloat(DissolveAmount, 1);
        float runTime = dissolveTime;
        
        while (0 < runTime)
        {
            m.SetFloat(DissolveAmount, runTime / dissolveTime);
            runTime -= 1;
            yield return null;
        }
        Destroy(gameObject);
        m.SetFloat(DissolveAmount, 1);
        runTime = 0;
    }
}
