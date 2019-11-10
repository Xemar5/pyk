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
    struct AnimationCurveSystemJob : IJobForEach<Translation, AnimationCurveComponent>
    {
        public int frame;
        
        public void Execute(ref Translation translation, [ReadOnly] ref AnimationCurveComponent curve)
        {
            translation.Value += translation.Value * curve.curve.samples[frame % curve.curve.samples.Length];
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new AnimationCurveSystemJob();
        job.frame = (int)(Time.time * 60);
        return job.Schedule(this, inputDependencies);
    }
}