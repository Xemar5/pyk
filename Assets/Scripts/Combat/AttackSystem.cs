using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

public class AttackSystem : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;
    private StepPhysicsWorld stepPhysicsWorld;
    private EndFramePhysicsSystem endFramePhysicsSystem;
    private EntityQuery attackerQuery;

    public struct AttackData
    {
        public int damage;
        public float attackDelay;
        public float3 velocity;
    }

    [BurstCompile]
    private struct AttackerDataGathererJob : IJobForEachWithEntity<AttackerComponent, PhysicsVelocity>
    {
        public NativeArray<Entity> entities;
        public NativeArray<AttackerComponent> attackers;
        public NativeArray<PhysicsVelocity> velocities;

        public void Execute(Entity entity, int index, ref AttackerComponent attacker, ref PhysicsVelocity velocity)
        {
            entities[index] = entity;
            attackers[index] = attacker;
            velocities[index] = velocity;
        }
    }
    [BurstCompile]
    private struct AttackerDataCombineJob : IJobParallelFor
    {
        public NativeHashMap<int, AttackData>.ParallelWriter attackersMap;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Entity> entities;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<PhysicsVelocity> velocities;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<AttackerComponent> attackers;
        public void Execute(int index)
        {
            attackersMap.TryAdd(entities[index].Index, new AttackData()
            {
                velocity = velocities[index].Linear,
                attackDelay = attackers[index].attackDelay,
                damage = attackers[index].damage,
            });
        }
    }


    private struct AttackCollisionJob : IContactsJob
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        [ReadOnly] public ComponentDataFromEntity<AttackerComponent> attackerDataFromEntity;
        [ReadOnly] public ComponentDataFromEntity<DefenderComponent> defenderDataFromEntity;
        [ReadOnly] public int attackersCount;
        [ReadOnly] public NativeHashMap<int, AttackData> attackersMap;


        public void Execute(ref ModifiableContactHeader header, ref ModifiableContactPoint contact)
        {
            if (attackerDataFromEntity.Exists(header.Entities.EntityA) == true &&
                defenderDataFromEntity.Exists(header.Entities.EntityB) == true)
            {
                AttackData attacker = attackersMap[header.Entities.EntityA.Index];
                commandBuffer.AddComponent(header.BodyIndexPair.BodyAIndex, header.Entities.EntityA, new UncontrolledMovementComponent()
                {
                    duration = attacker.attackDelay,
                    direction = header.Normal,
                    speed = math.length(attacker.velocity),
                });
                commandBuffer.AddComponent(header.BodyIndexPair.BodyBIndex, header.Entities.EntityB, new DamageComponent()
                {
                    attacker = header.Entities.EntityA,
                    damage = attacker.damage,
                });
            }
            if (attackerDataFromEntity.Exists(header.Entities.EntityB) == true &&
                defenderDataFromEntity.Exists(header.Entities.EntityA) == true)
            {
                AttackData attacker = attackersMap[header.Entities.EntityB.Index];
                commandBuffer.AddComponent(header.BodyIndexPair.BodyBIndex, header.Entities.EntityB, new UncontrolledMovementComponent()
                {
                    duration = attacker.attackDelay,
                    direction = header.Normal,
                    speed = math.length(attacker.velocity),
                });
                commandBuffer.AddComponent(header.BodyIndexPair.BodyAIndex, header.Entities.EntityA, new DamageComponent()
                {
                    attacker = header.Entities.EntityA,
                    damage = attacker.damage,
                });
            }
        }
    }

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        endFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
        attackerQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<AttackerComponent>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
            }
        });
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int attackerCount = attackerQuery.CalculateEntityCount();
        if (attackerCount == 0)
        {
            return inputDeps;
        }

        NativeArray<Entity> entities = new NativeArray<Entity>(attackerCount, Allocator.TempJob);
        NativeArray<AttackerComponent> attackers = new NativeArray<AttackerComponent>(attackerCount, Allocator.TempJob);
        NativeArray<PhysicsVelocity> velocities = new NativeArray<PhysicsVelocity>(attackerCount, Allocator.TempJob);
        NativeHashMap<int, AttackData> attackersMap = new NativeHashMap<int, AttackData>(attackerCount, Allocator.TempJob);

        JobHandle attackerGathererHandle = new AttackerDataGathererJob()
        {
            entities = entities,
            attackers = attackers,
            velocities = velocities,
        }.Schedule(attackerQuery, inputDeps);

        JobHandle attackerCombineHandle = new AttackerDataCombineJob()
        {
            entities = entities,
            attackers = attackers,
            velocities = velocities,
            attackersMap = attackersMap.AsParallelWriter(),
        }.Schedule(attackerCount, 64, attackerGathererHandle);

        JobHandle attackCollisionCallback(ref ISimulation simulation, ref PhysicsWorld world, JobHandle inputDependencies)
        {
            JobHandle collisionHandle = new AttackCollisionJob()
            {
                attackersMap = attackersMap,
                attackersCount = attackerCount,
                attackerDataFromEntity = GetComponentDataFromEntity<AttackerComponent>(true),
                defenderDataFromEntity = GetComponentDataFromEntity<DefenderComponent>(true),
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            }.Schedule(simulation, ref world, inputDependencies);
            return attackersMap.Dispose(collisionHandle);
        }
        stepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, attackCollisionCallback, attackerCombineHandle);



        return attackerCombineHandle;
    }
}
