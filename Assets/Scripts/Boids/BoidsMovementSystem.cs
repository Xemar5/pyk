using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

//[UpdateAfter(typeof(BoidsMovementDataUpdateSystem))]
//[UpdateBefore(typeof(EndFramePhysicsSystem))]

[UpdateInGroup(typeof(BoidsMovementSystemGroup))]
[UpdateAfter(typeof(BoidsMovementDataUpdateSystem))]
public class BoidsMovementSystem : JobComponentSystem
{
    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    private EntityQuery movementQuery;


    [BurstCompile]
    private struct CalculateAccelerationsJob : IJobForEachWithEntity<BoidData, Translation, PhysicsVelocity>
    {
        [ReadOnly] public float targetWeight;
        [ReadOnly] public float3 targetPosition;
        [ReadOnly] public BoidsSettings boidsSettings;
        public NativeArray<float3> accelerations;

        public void Execute(Entity entity, int index, ref BoidData boidData, ref Translation translation, ref PhysicsVelocity physicsVelocity)
        {
            float3 offsetToTarget = targetPosition - translation.Value;
            float3 acceleration = SteerTowards(offsetToTarget, boidData, physicsVelocity) * targetWeight;

            float3 collisionAvoidForce = SteerTowards(boidData.obstacleAvoidanceHeading, boidData, physicsVelocity);
            acceleration += collisionAvoidForce * boidsSettings.avoidanceWeight;

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
            accelerations[index] = acceleration;
        }

        private float3 SteerTowards(float3 vector, BoidData boidData, PhysicsVelocity physicsVelocity)
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

    }


    [BurstCompile]
    private struct MovementJob : IJobForEachWithEntity<BoidData, PhysicsVelocity>
    {
        [ReadOnly] public BoidsSettings boidsSettings;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float3> accelerations;

        public void Execute(Entity entity, int index, ref BoidData boidData, ref PhysicsVelocity physicsVelocity)
        {
            float3 velocity = physicsVelocity.Linear + accelerations[index];
            float3 dir = math.normalizesafe(velocity);
            float speed = math.length(velocity);
            speed = math.clamp(speed, boidsSettings.minSpeed, boidsSettings.maxSpeed);
            velocity = dir * speed;
            velocity.y = 0;

            physicsVelocity.Linear = velocity;
            physicsVelocity.Angular = new float3();
        }
    }



    protected override void OnCreate()
    {
        movementQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<BoidData>(),
                ComponentType.ReadOnly<Rotation>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UncontrolledMovementComponent>()
            }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        int entityCount = movementQuery.CalculateEntityCount();
        if (entityCount == 0)
        {
            // No boids found
            return inputDependencies;
        }

        float3 targetPosition = new float3(0, 0, 0);
        float targetWeight = 0;
        if (PlayerInput.Singleton && PlayerInput.Singleton.IsPositionHit)
        {
            targetPosition = PlayerInput.Singleton.MouseHitPosition;
            targetWeight = BoidHelper.boidSettings.targetWeight;
        }

        NativeArray<float3> accelerationsArray = new NativeArray<float3>(entityCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


        JobHandle calculateAccelerationHandle = new CalculateAccelerationsJob()
        {
            accelerations = accelerationsArray,
            boidsSettings = BoidHelper.boidSettings,
            targetPosition = targetPosition,
            targetWeight = targetWeight,
        }.Schedule(movementQuery, inputDependencies);

        JobHandle moveControlledJob = new MovementJob()
        {
            accelerations = accelerationsArray,
            boidsSettings = BoidHelper.boidSettings,
        }.Schedule(movementQuery, calculateAccelerationHandle);

        return moveControlledJob;
    }
}