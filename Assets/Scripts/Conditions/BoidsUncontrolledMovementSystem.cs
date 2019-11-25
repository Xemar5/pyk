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
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(BoidsMovementSystemGroup))]
[UpdateAfter(typeof(BoidsMovementDataUpdateSystem))]
public class BoidsUncontrolledMovementSystem : JobComponentSystem
{
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;
    private BoidsSettings boidsSettings;

    private struct UncontrolledMovementJob : IJobForEachWithEntity<UncontrolledMovementComponent, PhysicsVelocity>
    {
        public BoidsSettings boidsSettings;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float deltaTime;

        public void Execute(Entity entity, int index, ref UncontrolledMovementComponent uncontrolledMovement, ref PhysicsVelocity physicsVelocity)
        {
            uncontrolledMovement.duration -= deltaTime;
            if (uncontrolledMovement.duration <= 0)
            {
                commandBuffer.RemoveComponent<UncontrolledMovementComponent>(index, entity);
            }
            else
            {
                physicsVelocity.Linear = uncontrolledMovement.direction * uncontrolledMovement.speed;
            }
        }
    }



    protected override void OnCreate()
    {
        boidsSettings = Resources.Load<BoidsSettingsData>("BoidSettings").settings;
        commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new UncontrolledMovementJob()
        {
            commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            boidsSettings = boidsSettings,
            deltaTime = Time.DeltaTime,
        }.Schedule(this, inputDeps);

        commandBufferSystem.AddJobHandleForProducer(job);
        return job;
    }
}
