using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

//[UpdateAfter(typeof(BoidsMovementSystem))]
//[UpdateAfter(typeof(BuildPhysicsWorld))]
[UpdateInGroup(typeof(BoidsMovementSystemGroup))]
public unsafe class BoidsMovementDataUpdateSystem : JobComponentSystem
{
    private EntityQuery boidsQuery;
    private BlobAssetReference<Unity.Physics.Collider> sphereCollider;
    private BuildPhysicsWorld buildPhysicsWorldSystem;
    private EndFramePhysicsSystem endFramePhysicsSystem;

    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.

    [BurstCompile]
    private struct GetTranslationsJob : IJobForEachWithEntity<Translation>
    {
        public NativeArray<float3> positions;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            positions[index] = translation.Value;
        }
    }


    [BurstCompile]
    private struct GetRotationsJob : IJobForEachWithEntity<Rotation>
    {
        public NativeArray<quaternion> rotations;

        public void Execute(Entity entity, int index, [ReadOnly] ref Rotation rotation)
        {
            rotations[index] = rotation.Value;
        }
    }

    [BurstCompile]
    private struct UpdateBoidsObstacleAvoidanceJob : IJobForEachWithEntity<Translation, Rotation, BoidData>
    {
        [ReadOnly] public CollisionWorld physicsWorld;
        [ReadOnly] public BoidsSettings boidsSettings;
        [ReadOnly] public BlobAssetReference<Unity.Physics.Collider> collider;
        [ReadOnly] public BlobAssetReference<BlobArray<float3>> rayDirections;
        public NativeArray<float3> avoidances;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Rotation rotation, ref BoidData boidData)
        {
            if (IsHeadingForColision(translation.Value, translation.Value + math.forward(rotation.Value) * boidsSettings.avoidRadius) == true)
            {
                avoidances[index] = ObstacleRays(rotation, translation, boidsSettings);
            }
            else
            {
                avoidances[index] = new float3();
            }
        }

        private float3 ObstacleRays([ReadOnly] Rotation rotation, [ReadOnly] Translation translation, [ReadOnly] BoidsSettings settings)
        {

            for (int i = 0; i < rayDirections.Value.Length; i++)
            {
                float3 dir = mul(rotation.Value, rayDirections.Value[i]);
                if (IsHeadingForColision(translation.Value, translation.Value + dir * settings.avoidRadius) == false)
                {
                    return dir;
                }
            }
            return math.forward(rotation.Value);
        }

        private unsafe bool IsHeadingForColision(float3 rayFrom, float3 rayTo)
        {
            ColliderCastInput input = new ColliderCastInput()
            {
                Collider = (Unity.Physics.Collider*)collider.GetUnsafePtr(),
                Orientation = Unity.Mathematics.quaternion.identity,
                Start = rayFrom,
                End = rayTo,
            };
            return physicsWorld.CastCollider(input);
            //return false;
        }

    }


    [BurstCompile]
    private struct UpdateBoidsDataJob : IJobForEachWithEntity<Translation, Rotation, BoidData>
    {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float3> translations;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float3> avoidances;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<quaternion> rotations;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Rotation rotation, ref BoidData boidData)
        {

            boidData.numFlockmates = 0;
            boidData.flockHeading = new float3(0, 0, 0);
            boidData.flockCentre = new float3(0, 0, 0);
            boidData.avoidanceHeading = new float3(0, 0, 0);
            boidData.obstacleAvoidanceHeading = avoidances[index];

            for (int i = 0; i < translations.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }
                float3 offset = translations[i] - translation.Value;
                float sqrDst = math.lengthsq(offset);
                if (sqrDst > math.lengthsq(boidData.viewRadius))
                {
                    continue;
                }
                boidData.numFlockmates += 1;
                boidData.flockHeading += mul(rotations[i], new float3(0, 0, 1));
                boidData.flockCentre += translations[i];
                if (sqrDst > math.lengthsq(boidData.avoidRadius))
                {
                    continue;
                }

                if (sqrDst != 0)
                {
                    boidData.avoidanceHeading -= offset / sqrDst;
                }
            }
            if (boidData.numFlockmates != 0)
            {
                boidData.flockCentre = boidData.flockCentre / boidData.numFlockmates;
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

        SphereGeometry sphereGeometry = new SphereGeometry()
        {
            Center = new float3(0, 0, 0),
            Radius = BoidHelper.boidSettings.boundsRadius,
        };
        CollisionFilter filter = new CollisionFilter()
        {
            BelongsTo = 0b0100u,
            CollidesWith = 0b0001u, // obstacle layer
            GroupIndex = 0
        };
        sphereCollider = Unity.Physics.SphereCollider.Create(sphereGeometry, filter);
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        buildPhysicsWorldSystem = buildPhysicsWorldSystem ?? World.GetExistingSystem<BuildPhysicsWorld>();
        endFramePhysicsSystem = endFramePhysicsSystem ?? World.GetExistingSystem<EndFramePhysicsSystem>();
        if (buildPhysicsWorldSystem == null || endFramePhysicsSystem == null)
        {
            // BuildPhysicsWorld system is not yet created
            return inputDependencies;
        }

        int boidCount = boidsQuery.CalculateEntityCount();

        if (boidCount == 0)
        {
            // No boids found
            return inputDependencies;
        }

        JobHandle buildPhysicsSystemDependencies = JobHandle.CombineDependencies(inputDependencies, buildPhysicsWorldSystem.FinalJobHandle);

        NativeArray<float3> translationsArray = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float3> avoidancesArray = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<quaternion> rotationsArray = new NativeArray<quaternion>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle translationsJobHandle = new GetTranslationsJob()
        {
            positions = translationsArray
        }.Schedule(boidsQuery, buildPhysicsSystemDependencies);

        JobHandle rotationsJobHandle = new GetRotationsJob()
        {
            rotations = rotationsArray
        }.Schedule(boidsQuery, buildPhysicsSystemDependencies);

        JobHandle obstacleAvoidanceHandle = new UpdateBoidsObstacleAvoidanceJob()
        {
            boidsSettings = BoidHelper.boidSettings,
            collider = sphereCollider,
            rayDirections = BoidHelper.directions,
            avoidances = avoidancesArray,
            physicsWorld = buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld
        }.Schedule(boidsQuery, buildPhysicsSystemDependencies);


        JobHandle combineHandle = JobHandle.CombineDependencies(translationsJobHandle, rotationsJobHandle, obstacleAvoidanceHandle);


        JobHandle movementJobHandle = new UpdateBoidsDataJob()
        {
            translations = translationsArray,
            rotations = rotationsArray,
            avoidances = avoidancesArray,
        }.Schedule(boidsQuery, combineHandle);

        endFramePhysicsSystem.HandlesToWaitFor.Add(movementJobHandle);
        return movementJobHandle;
    }
}