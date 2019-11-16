using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(BoidsMoverSystem))]
public class BoidsPositionClampSystem : JobComponentSystem
{

    private EndSimulationEntityCommandBufferSystem commandBufferSystem;

    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    //[BurstCompile]
    private struct BoidsPositionClampSystemJob : IJobForEachWithEntity<Translation, BoidData, Rotation>
    {
        // Add fields here that your job needs to do its work.
        // For example,
        public float deltaTime;
        public float maxDistance;
        public EntityCommandBuffer.Concurrent commandBuffer;
        [ReadOnly]
        public ComponentDataFromEntity<UncontrolledMovementComponent> componentDataFromEntity;


        public void Execute(Entity entity, int index, ref Translation translation, ref BoidData boidData, ref Rotation rotation)
        {
            // Implement the work to perform for each entity here.
            // You should only access data that is local or that is a
            // field on this job. Note that the 'rotation' parameter is
            // marked as [ReadOnly], which means it cannot be modified,
            // but allows this job to run in parallel with other jobs
            // that want to read Rotation component data.
            // For example,
            //     translation.Value += mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
            bool addUncontrolledMovementComponent = false;
            if (translation.Value.x > maxDistance)
            {
                boidData.velocity.x = -boidData.velocity.x;
                translation.Value.x = maxDistance;
                addUncontrolledMovementComponent = true;
            }
            else if (translation.Value.x < -maxDistance)
            {
                boidData.velocity.x = -boidData.velocity.x;
                translation.Value.x = -maxDistance;
                addUncontrolledMovementComponent = true;
            }
            if (translation.Value.z > maxDistance)
            {
                boidData.velocity.z = -boidData.velocity.z;
                translation.Value.z = maxDistance;
                addUncontrolledMovementComponent = true;
            }
            else if (translation.Value.z < -maxDistance)
            {
                boidData.velocity.z = -boidData.velocity.z;
                translation.Value.z = -maxDistance;
                addUncontrolledMovementComponent = true;
            }

            if (addUncontrolledMovementComponent == true)
            {
                rotation.Value = quaternion.LookRotationSafe(boidData.velocity, new float3(0, 1, 0));
                if (componentDataFromEntity.Exists(entity) == false)
                {
                    commandBuffer.AddComponent(index, entity, new UncontrolledMovementComponent() { duration = 2 });
                }
                else
                {
                    commandBuffer.SetComponent(index, entity, new UncontrolledMovementComponent() { duration = 2 });
                }
            }

        }

    }

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new BoidsPositionClampSystemJob
        {

            // Assign values to the fields on your job here, so that it has
            // everything it needs to do its work when it runs later.
            // For example,
            deltaTime = UnityEngine.Time.deltaTime,
            maxDistance = 60,
            commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            componentDataFromEntity = GetComponentDataFromEntity<UncontrolledMovementComponent>(true),

        }.Schedule(this, inputDependencies);

        commandBufferSystem.AddJobHandleForProducer(job);


        // Now that the job is set up, schedule it to be run. 
        return job;
    }
}