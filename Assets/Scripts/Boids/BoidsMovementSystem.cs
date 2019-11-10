using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(BoidsMovementSystem))]
public class BoidsMovementSystem : JobComponentSystem
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
    struct MoveBoidsJob : IJobForEach<Translation, Rotation, BoidData>
    {
        public float deltaTime;
        [DeallocateOnJobCompletion] public NativeArray<float3> translations;
        [DeallocateOnJobCompletion] public NativeArray<quaternion> rotations;

        public void Execute(ref Translation translation, [ReadOnly] ref Rotation rotation, [ReadOnly] ref BoidData boidData)
        {

            translation.Value += mul(rotation.Value, float3(0, 0, 1)) * deltaTime * boidData.movementSpeed;
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


        JobHandle movementJobHandle = new MoveBoidsJob()
        {
            translations = translationsArray,
            rotations = rotationsArray,
            deltaTime = Time.deltaTime,
        }.Schedule(boidsQuery, combineHandle);

        

        return movementJobHandle;
    }
}