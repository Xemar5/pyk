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
    struct MoveBoidJob : IJobForEach<BoidData, Rotation, Translation>
    {
        [ReadOnly] public float deltaTime;
        public void Execute(ref BoidData boidData, ref Rotation rotation, ref Translation translation)
        {
            float3 acceleration = new float3(0, 0, 0);

            //TODO:code for target

            if (boidData.numFlockmates != 0)
            {
                float3 centreOfFlockmates = boidData.flockCentre / boidData.numFlockmates;
                float3 offsetToFlockmatesCentre = centreOfFlockmates - translation.Value;

                float3 alignmentForce = SteerTowards(boidData.flockHeading, boidData);
                float3 cohesionForce = SteerTowards(offsetToFlockmatesCentre, boidData);
                float3 separationForce = SteerTowards(boidData.avoidanceHeading, boidData);

                acceleration += alignmentForce * boidData.alignWeight;
                acceleration += cohesionForce * boidData.cohesionWeight;
                acceleration += separationForce * boidData.separationWeight;
                if (IsNan(acceleration))
                {
                    acceleration = new float3(0, 0, 0);
                }
            }

            
            //TODO:code for collision avoidance

            boidData.velocity += acceleration * deltaTime;
            boidData.velocity.y = 0;
            float speed = Magnitude(boidData.velocity);
            //float3 dir = new float3(0,0,0);
            //if (speed != 0)
            //{
            //  dir = boidData.velocity / speed;
            //}
            speed = clamp(speed, boidData.maxSpeed / 2, boidData.maxSpeed);
            float3 dir = new float3(0, 0, 0);
            if (Magnitude(boidData.velocity)!= 0)
            {
                dir = normalize(boidData.velocity);
            }



            boidData.velocity = dir * speed;

            if (IsNan(boidData.velocity)){
                boidData.velocity = new float3(0, 0, 0);
            }

            if (dir.x != 0 || dir.y != 0 || dir.z != 0)
            {
                rotation.Value = Unity.Mathematics.quaternion.LookRotation(dir, new float3(0, 1, 0));
            }

            translation.Value += boidData.velocity * deltaTime;
            if (translation.Value.x > 20)
            {
                translation.Value.x = -20;
            }
            else if (translation.Value.x < -20)
            {
                translation.Value.x = 20;
            }
            if (translation.Value.z > 20)
            {
                translation.Value.z = -20;
            }
            else if (translation.Value.z < -20)
            {
                translation.Value.z = 20;
            }


        }

        bool IsNan(float3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }

        float Magnitude(float3 v)
        {
            return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        float3 SteerTowards(float3 vector, BoidData boidData)
        {
            if (Magnitude(vector) == 0) return vector;
            float3 v = normalize(vector) * boidData.maxSpeed - boidData.velocity;
            if (Magnitude(v) > boidData.maxSpeed)
            {
                v = normalize(v) * boidData.maxSpeed;
            }
            return v; //check if it's correct
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new MoveBoidJob()
        {
            deltaTime = Time.deltaTime,
        };

        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     job.deltaTime = UnityEngine.Time.deltaTime;



        // Now that the job is set up, schedule it to be run. 
        return job.Schedule(this, inputDependencies);
    }
}