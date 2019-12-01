using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

//[UpdateAfter(typeof(BoidsMovementDataUpdateSystem))]
//[UpdateBefore(typeof(EndFramePhysicsSystem))]

[UpdateInGroup(typeof(BoidsMovementSystemGroup))]
[UpdateAfter(typeof(BoidsMovementDataUpdateSystem))]
public class BoidsMovementSystem : JobComponentSystem
{

    private EntityQuery followQuery;
    private EntityQuery repelQuery;
    private EntityQuery avoidQuery;
    private EntityQuery flockQuery;
    private EntityQuery movementQuery;


    //[BurstCompile]
    //private struct AccelerationResetJob : IJobForEachWithEntity<BoidData>
    //{
    //    public void Execute(Entity entity, int index, ref BoidData boidData)
    //    {
    //        boidData.acceleration = float3.zero;
    //    }
    //}

    [BurstCompile]
    private struct CalculateFollowAccelerationsJob : IJobForEachWithEntity<BoidData, Translation, SelectableComponent, PhysicsVelocity>
    {
        public BoidsSettings boidsSettings;
        public NativeHashMap<Entity, float3>.ParallelWriter accelerations;

        public void Execute(Entity entity, int index, [ReadOnly] ref BoidData boidData, [ReadOnly] ref Translation translation, [ReadOnly] ref SelectableComponent selectable, [ReadOnly] ref PhysicsVelocity physicsVelocity)
        {
            accelerations.TryAdd(entity, SteerTowards(selectable.selectorTranslation - translation.Value, boidData, physicsVelocity) * boidsSettings.targetWeight);
        }
    }

    [BurstCompile]
    private struct CalculateRepelAccelerationsJob : IJobForEachWithEntity<BoidData, Translation, SelectableComponent, PhysicsVelocity>
    {
        public BoidsSettings boidsSettings;
        public NativeHashMap<Entity, float3>.ParallelWriter accelerations;

        public void Execute(Entity entity, int index, [ReadOnly] ref BoidData boidData, [ReadOnly] ref Translation translation, [ReadOnly] ref SelectableComponent selectable, [ReadOnly] ref PhysicsVelocity physicsVelocity)
        {
            accelerations.TryAdd(entity, SteerTowards(translation.Value - selectable.selectorTranslation, boidData, physicsVelocity) * boidsSettings.avoidStateWeight);
        }
    }

    [BurstCompile]
    private struct CalculateAvoidAccelerationsJob : IJobForEachWithEntity<BoidData, Translation, PhysicsVelocity>
    {
        public BoidsSettings boidsSettings;
        public NativeHashMap<Entity, float3>.ParallelWriter accelerations;

        public void Execute(Entity entity, int index, [ReadOnly] ref BoidData boidData, [ReadOnly] ref Translation translation, [ReadOnly] ref PhysicsVelocity physicsVelocity)
        {
            accelerations.TryAdd(entity, SteerTowards(boidData.obstacleAvoidanceHeading, boidData, physicsVelocity) * boidsSettings.avoidanceWeight);
        }


    }
    [BurstCompile]
    private struct CalculateFlockAccelerationsJob : IJobForEachWithEntity<BoidData, Translation, PhysicsVelocity>
    {
        public BoidsSettings boidsSettings;
        public NativeHashMap<Entity, float3>.ParallelWriter accelerations;

        public void Execute(Entity entity, int index, [ReadOnly] ref BoidData boidData, [ReadOnly] ref Translation translation, [ReadOnly] ref PhysicsVelocity physicsVelocity)
        {
            float3 acceleration = float3.zero;

            if (boidData.numFlockmates != 0)
            {
                float3 offsetToFlockmatesCentre = boidData.flockCentre - translation.Value;
                float3 alignmentForce = SteerTowards(boidData.flockHeading, boidData, physicsVelocity);
                float3 cohesionForce = SteerTowards(offsetToFlockmatesCentre, boidData, physicsVelocity);
                float3 separationForce = SteerTowards(boidData.avoidanceHeading, boidData, physicsVelocity);

                acceleration += alignmentForce * boidsSettings.alignWeight;
                acceleration += cohesionForce * boidsSettings.cohesionWeight;
                acceleration += separationForce * boidsSettings.separationWeight;
            }

            accelerations.TryAdd(entity, acceleration);
        }


    }


    [BurstCompile]
    private struct MovementJob : IJobForEachWithEntity<BoidData, PhysicsVelocity>
    {
        public BoidsSettings boidsSettings;
        [ReadOnly] public NativeHashMap<Entity, float3> followAccelerations;
        [ReadOnly] public NativeHashMap<Entity, float3> repelAccelerations;
        [ReadOnly] public NativeHashMap<Entity, float3> avoidAccelerations;
        [ReadOnly] public NativeHashMap<Entity, float3> flockAccelerations;

        public void Execute(Entity entity, int index, [ReadOnly] ref BoidData boidData, ref PhysicsVelocity physicsVelocity)
        {
            followAccelerations.TryGetValue(entity, out float3 followAcceleration);
            repelAccelerations.TryGetValue(entity, out float3 repelAcceleration);
            avoidAccelerations.TryGetValue(entity, out float3 avoidAcceleration);
            flockAccelerations.TryGetValue(entity, out float3 flockAcceleration);

            float3 velocity = physicsVelocity.Linear + followAcceleration + repelAcceleration + avoidAcceleration + flockAcceleration;
            float3 dir = math.normalizesafe(velocity);
            float speed = math.length(velocity);
            speed = math.clamp(speed, boidsSettings.minSpeed, boidsSettings.maxSpeed);
            velocity = dir * speed;
            velocity.y = 0;

            physicsVelocity.Linear = velocity;
            physicsVelocity.Angular = new float3();
        }
    }



    private static float3 SteerTowards(float3 vector, BoidData boidData, PhysicsVelocity physicsVelocity)
    {
        float3 maxVelocity = math.normalizesafe(vector) * boidData.maxSpeed;
        float3 targetVelocity = maxVelocity - physicsVelocity.Linear;
        if (math.lengthsq(targetVelocity) > math.lengthsq(boidData.maxSteerForce))
        {
            return math.normalizesafe(targetVelocity) * boidData.maxSteerForce;
        }
        else
        {
            return targetVelocity;
        }
    }



    protected override void OnCreate()
    {
        followQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<BoidData>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<SelectableComponent>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
                typeof(SelectableGroupComponent),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UncontrolledMovementComponent>()
            }
        });
        followQuery.AddSharedComponentFilter(new SelectableGroupComponent()
        {
            id = SelectorID.Attract,
        });

        repelQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<BoidData>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
                ComponentType.ReadOnly<SelectableComponent>(),
                typeof(SelectableGroupComponent),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UncontrolledMovementComponent>()
            }
        });
        repelQuery.AddSharedComponentFilter(new SelectableGroupComponent()
        {
            id = SelectorID.Repel,
        });

        avoidQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<BoidData>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
            },
        });

        flockQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<BoidData>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UncontrolledMovementComponent>()
            }
        });

        movementQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<BoidData>(),
                ComponentType.ReadWrite<PhysicsVelocity>(),
            },
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        JobHandle combineAccelerationHandle = inputDependencies;

        int followCount = followQuery.CalculateEntityCount();
        int repelCount = repelQuery.CalculateEntityCount();
        int avoidCount = avoidQuery.CalculateEntityCount();
        int flockCount = flockQuery.CalculateEntityCount();
        NativeHashMap<Entity, float3> followAccelerations = new NativeHashMap<Entity, float3>(followCount, Allocator.TempJob);
        NativeHashMap<Entity, float3> repelAccelerations = new NativeHashMap<Entity, float3>(repelCount, Allocator.TempJob);
        NativeHashMap<Entity, float3> avoidAccelerations = new NativeHashMap<Entity, float3>(avoidCount, Allocator.TempJob);
        NativeHashMap<Entity, float3> flockAccelerations = new NativeHashMap<Entity, float3>(flockCount, Allocator.TempJob);

        if (followCount > 0)
        {
            JobHandle handle = new CalculateFollowAccelerationsJob()
            {
                boidsSettings = BoidHelper.boidSettings,
                accelerations = followAccelerations.AsParallelWriter(),
            }.Schedule(followQuery, inputDependencies);
            combineAccelerationHandle = JobHandle.CombineDependencies(combineAccelerationHandle, handle);
        }

        if (repelCount > 0)
        {
            JobHandle handle = new CalculateRepelAccelerationsJob()
            {
                boidsSettings = BoidHelper.boidSettings,
                accelerations = repelAccelerations.AsParallelWriter(),
            }.Schedule(repelQuery, inputDependencies);
            combineAccelerationHandle = JobHandle.CombineDependencies(combineAccelerationHandle, handle);
        }

        if (avoidCount > 0)
        {
            JobHandle handle = new CalculateAvoidAccelerationsJob()
            {
                boidsSettings = BoidHelper.boidSettings,
                accelerations = avoidAccelerations.AsParallelWriter(),
            }.Schedule(avoidQuery, inputDependencies);
            combineAccelerationHandle = JobHandle.CombineDependencies(combineAccelerationHandle, handle);
        }

        if (flockCount > 0)
        {
            JobHandle handle = new CalculateFlockAccelerationsJob()
            {
                boidsSettings = BoidHelper.boidSettings,
                accelerations = flockAccelerations.AsParallelWriter(),
            }.Schedule(flockQuery, inputDependencies);
            combineAccelerationHandle = JobHandle.CombineDependencies(combineAccelerationHandle, handle);
        }


        JobHandle moveControlledJob = new MovementJob()
        {
            followAccelerations = followAccelerations,
            repelAccelerations = repelAccelerations,
            avoidAccelerations = avoidAccelerations,
            flockAccelerations = flockAccelerations,
            boidsSettings = BoidHelper.boidSettings,
        }.Schedule(movementQuery, combineAccelerationHandle);

        followAccelerations.Dispose(moveControlledJob);
        repelAccelerations.Dispose(moveControlledJob);
        avoidAccelerations.Dispose(moveControlledJob);
        flockAccelerations.Dispose(moveControlledJob);

        return moveControlledJob;
    }
}