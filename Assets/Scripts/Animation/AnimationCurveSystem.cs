using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class AnimationCurveSystem : JobComponentSystem
{
    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    struct AnimationCurveSystemJob : IJobForEach<Translation, AnimationCurveTranslation>
    {
        [ReadOnly] public float time;
        
        public void Execute(ref Translation translation, ref AnimationCurveTranslation curve)
        {
            float3 newTranslation = float3.zero;
            int frame = (int)(time * curve.fps);
            if (curve.frameDelay == -1)
            {
                curve.frameDelay = frame;
            }

            if (curve.xCurve.Value.samples.Length > 0)
            {
                newTranslation.x = curve.xCurve.Value.samples[(frame - curve.frameDelay) % curve.xCurve.Value.samples.Length];
            }                                                       
            if (curve.yCurve.Value.samples.Length > 0)              
            {                                                       
                newTranslation.y = curve.yCurve.Value.samples[(frame - curve.frameDelay) % curve.yCurve.Value.samples.Length];
            }                                                       
            if (curve.zCurve.Value.samples.Length > 0)              
            {                                                       
                newTranslation.z = curve.zCurve.Value.samples[(frame - curve.frameDelay) % curve.zCurve.Value.samples.Length];
            }
            translation.Value = newTranslation;
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new AnimationCurveSystemJob
        {
            time = UnityEngine.Time.time,
        };
        return job.Schedule(this, inputDependencies);
    }
}