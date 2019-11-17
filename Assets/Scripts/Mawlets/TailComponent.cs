using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct TailComponent : IComponentData
{

    public Entity target;
    public float3 targetOffset;

}
