using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(BoidsMovementDataUptadeSystem))]
public class BoidsMoverSystem : JobComponentSystem
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
    [ExcludeComponent(typeof(UncontrolledMovementComponent))]
    private struct MoveBoidJob : IJobForEach<BoidData, Rotation, Translation>
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public BoidsSettings boidsSettings;
        [ReadOnly] public float3 targetPos;
        public void Execute(ref BoidData boidData, ref Rotation rotation, ref Translation translation)
        {
            float3 acceleration = new float3(0, 0, 0);

            if (IsNan(targetPos) == false)
            {
                float3 offsetToTarget = targetPos - translation.Value;
                acceleration = SteerTowards(offsetToTarget, boidData) * boidsSettings.targetWeight;
            }

            //TODO:code for target

            if (boidData.numFlockmates != 0)
            {
                float3 centreOfFlockmates = boidData.flockCentre / boidData.numFlockmates;
                float3 offsetToFlockmatesCentre = centreOfFlockmates - translation.Value;

                float3 alignmentForce = SteerTowards(boidData.flockHeading, boidData);
                float3 cohesionForce = SteerTowards(offsetToFlockmatesCentre, boidData);
                float3 separationForce = SteerTowards(boidData.avoidanceHeading, boidData);

                acceleration += alignmentForce * boidsSettings.alignWeight;
                acceleration += cohesionForce * boidsSettings.cohesionWeight;
                acceleration += separationForce * boidsSettings.separationWeight;
            }


            //TODO:code for collision avoidance

            boidData.velocity += acceleration;
            boidData.velocity.y = 0;

            float3 dir = normalizesafe(boidData.velocity);
            float speed = length(boidData.velocity);
            speed = clamp(speed, boidsSettings.minSpeed, boidsSettings.maxSpeed);
            boidData.velocity = dir * speed;

            rotation.Value = Unity.Mathematics.quaternion.LookRotationSafe(dir, new float3(0, 1, 0));
            translation.Value += boidData.velocity * deltaTime;
        }

        private bool IsNan(float3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }


        private float3 SteerTowards(float3 vector, BoidData boidData)
        {
            float3 maxVelocity = normalizesafe(vector) * boidData.maxSpeed;
            float3 targetVelocity = maxVelocity - boidData.velocity;
            if (lengthsq(targetVelocity) > lengthsq(boidData.maxSteerForce))
            {
                return normalizesafe(targetVelocity) * boidData.maxSteerForce;
            }
            else
            {
                return targetVelocity;
            }
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(UncontrolledMovementComponent))]
    private struct MoveBoidUncontrolledJob : IJobForEach<BoidData, Translation>
    {
        [ReadOnly] public float deltaTime;

        public void Execute([ReadOnly] ref BoidData boidData, ref Translation translation)
        {
            translation.Value += boidData.velocity * deltaTime;
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        BoidsSettings boidsSettings = Resources.Load<BoidsSettingsData>("BoidSettings").settings;


        float3 targetPos = new float3(float.NaN, float.NaN, float.NaN);

        if (PlayerInput.Singleton && PlayerInput.Singleton.IsPositionHit)
        {
            targetPos = PlayerInput.Singleton.MouseHitPosition;
        }

        float deltaTime = Time.deltaTime;
        var moveControlledJob = new MoveBoidJob()
        {
            deltaTime = deltaTime,
            boidsSettings = boidsSettings,
            targetPos = targetPos
        }.Schedule(this, inputDependencies);

        var moveUncontrolledJob = new MoveBoidUncontrolledJob()
        {
            deltaTime = deltaTime,
        }.Schedule(this, moveControlledJob);

        //JobHandle jobHandle = JobHandle.CombineDependencies(moveControlledJob, moveUncontrolledJob);

        return moveUncontrolledJob;
    }
}