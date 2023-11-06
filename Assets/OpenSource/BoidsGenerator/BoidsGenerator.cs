using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidsGenerator : MonoBehaviour
{
    [System.Serializable]
    struct SpawnArea
    {
        public float _min, _max;
    }

    [Header("Boids")]
    [SerializeField]
    Transform _boids;
    [SerializeField]
    float _speed = 5f;
    [SerializeField]
    LayerMask _boidsLayer;

    [Header("Range")]
    [SerializeField, Range(0, 100f)]
    float _detectRange = 10f;
    [SerializeField, Range(0, 100f)]
    float _separationRange = 5f;

    [Header("Spawn")]
    [SerializeField, Range(1, 1000)]
    int _spawnCount;
    [SerializeField]
    SpawnArea _spawnAreaPosX;
    [SerializeField]
    SpawnArea _spawnAreaPosY;
    [SerializeField]
    SpawnArea _spawnAreaPosZ;

    [SerializeField] private Vector3 maxPos;
    [SerializeField] private Vector3 minPos;
    [SerializeField] private bool currentState;
    List<Transform> _boidAgents = new();

    /// <summary>
    /// 정렬
    /// </summary>
    AlignmentRule _alignmentRule = new();
    /// <summary>
    /// 응집력
    /// </summary>
    CohesionRule _cohesionRule = new();
    /// <summary>
    /// 분리
    /// </summary>
    SeparationRule _separtionRule = new();

    private void Awake()
    {
        SpawnBoids();
    }

    void SpawnBoids()
    {
        for (int i = 0; i < _spawnCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(_spawnAreaPosX._min, _spawnAreaPosX._max),
                Random.Range(_spawnAreaPosY._min, _spawnAreaPosY._max),
                Random.Range(_spawnAreaPosZ._min, _spawnAreaPosZ._max));

            _boidAgents.Add(Instantiate(_boids,spawnPos,Quaternion.identity));
        }
    }

    private void Update()
    {
        foreach (var agent in _boidAgents)
        {
            Vector3 dir = _cohesionRule.GetDirection(agent, GetNeighbor(agent, _detectRange));
            
            if (agent.position.x > maxPos.x || agent.position.y > maxPos.y || agent.position.z > maxPos.z)
                currentState = true;
            else if (agent.position.x < minPos.x || agent.position.y < minPos.y || agent.position.z < minPos.z)
                currentState = false;
            
            // if (currentState)
            //     dir -= _alignmentRule.GetDirection(agent, GetNeighbor(agent, _detectRange));
            // else if(currentState)
                dir += _alignmentRule.GetDirection(agent, GetNeighbor(agent, _detectRange));
            
            dir += _separtionRule.GetDirection(agent, GetNeighbor(agent, _separationRange));
            
            dir = Vector3.Lerp(agent.transform.forward, dir, Time.deltaTime);
            dir.Normalize();
            
            agent.transform.position += dir * (_speed * Time.deltaTime);
            agent.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    List<Transform> GetNeighbor(Transform agent, float range)
    { 
        var overlaps = Physics.OverlapSphere(agent.position, range, _boidsLayer);

        if(overlaps.Length == 0)
            return null;

        List<Transform> tf = new List<Transform>(overlaps.Length);

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i].transform == agent)
                continue;

            tf.Add(overlaps[i].transform);
        }

        return tf;
    }

    bool CalMaxDis(Vector3 origin, Vector3 MaxDis)
    {
        if (origin.x > MaxDis.x || origin.y > MaxDis.y || origin.z > MaxDis.z)
            return true;
        else
        {
            return false;
        }
    }
    
    bool CalMinsDis(Vector3 origin, Vector3 MinDis)
    {
        if (origin.x < MinDis.x || origin.y < MinDis.y || origin.z < MinDis.z)
            return true;
        else
        {
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(transform.position, maxPos);
    }
}