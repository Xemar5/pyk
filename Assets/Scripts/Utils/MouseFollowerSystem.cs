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
using Unity.Transforms;

public class MouseFollowerSystem : JobComponentSystem
{
    [BurstCompile]
    private struct MouseFollowerJob : IJobForEach<Translation, MouseFollowerComponent>
    {
        public float3 mousePosition;

        public void Execute(ref Translation translation, [ReadOnly] ref MouseFollowerComponent mouseFollower)
        {
            translation.Value = math.lerp(translation.Value, mousePosition, mouseFollower.weight);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new MouseFollowerJob()
        {
            mousePosition = PlayerInput.Singleton.MouseHitPosition,
        }.Schedule(this, inputDeps);
    }
}