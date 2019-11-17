using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct UnitSpawnerComponent : IComponentData
{

    public Entity headPrefab;
    public Entity tailPrefab;
    public int tailLength;
    public float3 tailOffset;

    public int spawnCount;
    public float3 translation;
    public quaternion rotation;
}
