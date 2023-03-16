using UnityEngine;
using Random = UnityEngine.Random;

public class RandomAddForce : MonoBehaviour
{
    public float randRange = 100;
    private void Start()
    {
        Rigidbody rigi = GetComponent<Rigidbody>();
        rigi.AddForce(new Vector3(Random.Range(-randRange, randRange),0,Random.Range(-randRange, randRange)));
    }
}
