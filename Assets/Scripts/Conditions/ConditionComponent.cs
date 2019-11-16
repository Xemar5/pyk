using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ConditionComponent : IComponentData
{

    // The duration left for this condition
    public float duration;
    // The entity this condition is applied to
    public Entity target;

}
