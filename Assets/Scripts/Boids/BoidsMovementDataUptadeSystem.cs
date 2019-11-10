using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

//[UpdateAfter(typeof(BoidsMovementSystem))]
public class BoidsMovementDataUptadeSystem : JobComponentSystem
{
    private EntityQuery boidsQuery;


    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.

    [BurstCompile]
    struct GetTranslationsJob : IJobForEachWithEntity<Translation>
    {
        public NativeArray<float3> positions;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            positions[index] = translation.Value;
        }
    }


    [BurstCompile]
    struct GetRotationsJob : IJobForEachWithEntity<Rotation>
    {
        public NativeArray<quaternion> rotations;

        public void Execute(Entity entity, int index, [ReadOnly] ref Rotation translation)
        {
            rotations[index] = translation.Value;
        }
    }

    //[BurstCompile]
    //struct GetBoidDataJob : IJobForEachWithEntity<BoidData>
    //{
    //    public NativeArray<quaternion> positions;

    //    public void Execute(Entity entity, int index, ref BoidData boidData)
    //    {
    //        positions[index] = boidData.acceleration;
    //    }
    //}

    [BurstCompile]
    struct UpdateBoidsDataJob : IJobForEachWithEntity<Translation, Rotation, BoidData>
    {
        [DeallocateOnJobCompletion,ReadOnly] public NativeArray<float3> translations;
        [DeallocateOnJobCompletion,ReadOnly] public NativeArray<quaternion> rotations;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Rotation rotation, ref BoidData boidData)
        {

            boidData.numFlockmates = 0;
            //translation.Value += mul(rotation.Value, float3(0, 0, 1)) * deltaTime * boidData.movementSpeed;
            for (int i = 0; i< translations.Length; i++)
            {
                if (i != index)
                {
                    float3 offset = translations[i] - translation.Value;
                    float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;
                    if (sqrDst < boidData.viewRadius * boidData.viewRadius)
                    {
                        boidData.numFlockmates += 1;
                        boidData.flockHeading += mul(rotations[i], float3(0, 0, 1)); //check if it's working
                        boidData.flockCentre += translations[i];
                        if (sqrDst < boidData.avoidRadius * boidData.avoidRadius)
                        {
                            boidData.avoidanceHeading -= offset/sqrDst;
                        }
                    }
                }
            }
        }
    }




    protected override void OnCreate()
    {
        boidsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadWrite<BoidData>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Rotation>(),
            }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,

        int boidCount = boidsQuery.CalculateEntityCount();

        if (boidCount == 0)
        {
            // No boids found
            return inputDependencies;
        }

        NativeArray<float3> translationsArray = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<quaternion> rotationsArray = new NativeArray<quaternion>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle translationsJobHandle = new GetTranslationsJob()
        {
            positions = translationsArray
        }.Schedule(boidsQuery, inputDependencies);

        JobHandle rotationsJobHandle = new GetRotationsJob()
        {
            rotations = rotationsArray
        }.Schedule(boidsQuery, inputDependencies);


        JobHandle combineHandle = JobHandle.CombineDependencies(translationsJobHandle, rotationsJobHandle);


        JobHandle movementJobHandle = new UpdateBoidsDataJob()
        {
            translations = translationsArray,
            rotations = rotationsArray,
            //deltaTime = Time.deltaTime,
        }.Schedule(boidsQuery, combineHandle);

        

        return movementJobHandle;
    }
}