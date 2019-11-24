using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(BoidsMovementSystemGroup))]
[UpdateAfter(typeof(BoidsPositionClampSystem))]
public class BoidsAdjustHeadSystem : JobComponentSystem
{
    [BurstCompile]
    private struct AdjustHeadJob : IJobForEachWithEntity<Translation, Rotation, PhysicsVelocity>
    {
        public void Execute(Entity entity, int index, ref Translation translation, ref Rotation rotation, ref PhysicsVelocity velocity)
        {
            translation.Value.y = 0;
            rotation.Value = quaternion.LookRotationSafe(velocity.Linear, new float3(0, 1, 0));
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle headAdjustmentHandle = new AdjustHeadJob().Schedule(this, inputDeps);
        return headAdjustmentHandle;
    }


}
