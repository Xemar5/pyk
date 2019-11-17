using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
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

            if (IsHeadingForColision(translation.Value, mul(rotation.Value, new float3(0, 0, 1)) * boidsSettings.avoidRadius, boidsSettings.boundsRadius))
            {
                float3 collisionAvoidDir = ObstacleRays(rotation, translation, boidsSettings);
                float3 collisionAvoidForce = SteerTowards(collisionAvoidDir,boidData) * boidsSettings.avoidanceWeight;
                acceleration += collisionAvoidForce;
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

        float3 ObstacleRays(Rotation rotation, Translation translation, BoidsSettings settings)
        {
            float3[] rayDirections = BoidHelper.directions;
            for (int i = 0; i < rayDirections.Length; i++)
            {
                float3 dir = mul(rotation.Value, rayDirections[i]);
                if (!IsHeadingForColision(translation.Value, translation.Value + dir * settings.avoidRadius, settings.boundsRadius))
                {
                    return dir;
                }
            }
            return mul(rotation.Value, float3(0, 0, 1));
        }

        bool IsHeadingForColision(float3 rayFrom, float3 rayTo, float radius)
        {
            Entity obstacle = SphereCast(rayFrom, rayTo, radius);
            if (obstacle == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public unsafe Entity SphereCast(float3 RayFrom, float3 RayTo, float radius)
        {
            var physicsWorldSystem = Unity.Entities.World.Active.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

            var filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = 0b1, // obstacle layer
                GroupIndex = 0
            };

            SphereGeometry sphereGeometry = new SphereGeometry()
            {
                Center = new Unity.Mathematics.float3(0, 0, 0),
                Radius = radius,
            };

            BlobAssetReference<Unity.Physics.Collider> sphereCollider = Unity.Physics.SphereCollider.Create(sphereGeometry, filter);

            ColliderCastInput input = new ColliderCastInput()
            {
                Collider = (Unity.Physics.Collider*)sphereCollider.GetUnsafePtr(),
                Orientation = Unity.Mathematics.quaternion.identity,
                Start = RayFrom,
                End = RayTo
            };

            ColliderCastHit hit = new ColliderCastHit();
            bool haveHit = collisionWorld.CastCollider(input, out hit);
            if (haveHit)
            {
                // see hit.Position 
                // see hit.SurfaceNormal
                Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                return e;
            }
            return Entity.Null;
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


        return moveControlledJob;
    }
}