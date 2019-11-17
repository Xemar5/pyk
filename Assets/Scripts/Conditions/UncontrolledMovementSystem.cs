using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class UncontrolledMovementSystem : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

    private struct UncontrolledMovementJob : IJobForEachWithEntity<UncontrolledMovementComponent, BoidData, Translation>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float deltaTime;

        public void Execute(Entity entity, int index, ref UncontrolledMovementComponent uncontrolledMovement, [ReadOnly] ref BoidData boidData, ref Translation translation)
        {
            uncontrolledMovement.duration -= deltaTime;
            if (uncontrolledMovement.duration <= 0)
            {
                commandBuffer.RemoveComponent<UncontrolledMovementComponent>(index, entity);
            }
            else
            {
                translation.Value += boidData.velocity * deltaTime;
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
