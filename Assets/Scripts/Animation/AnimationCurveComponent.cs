using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct AnimationCurveComponent : IComponentData
{

    public AnimationCurveExtention.AnimationCurveSampled curve;
    
}
