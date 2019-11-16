using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

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
                Entity newEntity = commandBuffer.Instantiate(index, unitSpawn.prefab);
                Translation translation = new Translation()
                {
                    Value = unitSpawn.translation + float3(random.NextFloat(-20, 20), 0, random.NextFloat(-20, 20)),
                };
                commandBuffer.SetComponent(index, newEntity, translation);
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
                random = new Unity.Mathematics.Random((uint)Mathf.RoundToInt(Time.realtimeSinceStartup * 100) + 1),
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