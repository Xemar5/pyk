using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct UncontrolledMovementComponent : IComponentData
{

    public float duration;
    public float3 direction;
    public float speed;

}
