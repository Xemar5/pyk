using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public class UncontrolledMovementSystem : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

    //[BurstCompile]
    private struct UncontrolledMovementJob : IJobForEachWithEntity<UncontrolledMovementComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float deltaTime;

        public void Execute(Entity entity, int index, ref UncontrolledMovementComponent uncontrolledMovement)
        {
            uncontrolledMovement.duration -= deltaTime;
            if (uncontrolledMovement.duration <= 0)
            {
                commandBuffer.RemoveComponent<UncontrolledMovementComponent>(index, entity);
            }
        }
    }

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new UncontrolledMovementJob()
        {
            commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            deltaTime = Time.deltaTime,
        }.Schedule(this, inputDeps);

        commandBufferSystem.AddJobHandleForProducer(job);
        return job;
    }
}
