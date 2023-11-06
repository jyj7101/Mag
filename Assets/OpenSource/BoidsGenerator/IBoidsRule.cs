
using System.Collections.Generic;
using UnityEngine;

public interface IBoidsRule
{
    public abstract Vector3 GetDirection(Transform agent, List<Transform> neighbor);

}
