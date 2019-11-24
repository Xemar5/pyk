using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class UnitSpawnerSystem : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem commandBufferSystem;


    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    struct UnitSpawnerSystemJob : IJobForEachWithEntity<UnitSpawnerComponent>
    {

        public EntityCommandBuffer.Concurrent commandBuffer;
        public Unity.Mathematics.Random random;
        
        
        public void Execute(Entity entity, int index, ref UnitSpawnerComponent unitSpawn)
        {
            for (int i = 0; i < unitSpawn.spawnCount; i++)
            {
                Entity head = commandBuffer.Instantiate(index, unitSpawn.headPrefab);
                Translation translation = new Translation()
                {
                    Value = unitSpawn.translation + math.float3(random.NextFloat(-20, 20), 0, random.NextFloat(-20, 20)),
                };
                Rotation rotation = new Rotation()
                {
                    Value = math.mul(unitSpawn.rotation, quaternion.Euler(0, random.NextFloat(0, math.PI * 2), 0)),
                };
                TailComponent headTail = new TailComponent()
                {
                    target = Entity.Null,
                    targetOffset = float3.zero,
                };
                commandBuffer.SetComponent(index, head, translation);
                commandBuffer.SetComponent(index, head, rotation);
                commandBuffer.AddComponent(index, head, headTail);
                for (int j = 0; j < unitSpawn.tailLength; j++)
                {
                    Entity tail = commandBuffer.Instantiate(index, unitSpawn.tailPrefab);
                    TailComponent tailComponent = new TailComponent()
                    {
                        target = head,
                        targetOffset = unitSpawn.tailOffset,
                    };
                    Translation tailTranslation = new Translation()
                    {
                        Value = translation.Value + math.mul(rotation.Value, unitSpawn.tailOffset),
                    };
                    Rotation tailRotation = new Rotation()
                    {
                        Value = rotation.Value,
                    };
                    commandBuffer.AddComponent(index, tail, tailComponent);
                    commandBuffer.SetComponent(index, tail, tailTranslation);
                    commandBuffer.SetComponent(index, tail, tailRotation);
                    head = tail;
                    translation = tailTranslation;
                }
            }
            //unitSpawn.spawnCount = 0;
        }
    }


    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        if (Input.GetKeyDown(KeyCode.Space) == true)
        {
            var job = new UnitSpawnerSystemJob()
            {
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                random = new Unity.Mathematics.Random((uint)Mathf.RoundToInt(UnityEngine.Time.realtimeSinceStartup * 100) + 1),
            }.Schedule(this, inputDependencies);

            commandBufferSystem.AddJobHandleForProducer(job);

            return job;
        }
        else
        {
            return inputDependencies;
        }
    }
}