using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(MouseFollowerSystem))]
[UpdateBefore(typeof(BoidsMovementSystemGroup))]
public class SelectionSystem : JobComponentSystem
{
    private EntityQuery selectorQuery;
    private EndInitializationEntityCommandBufferSystem commandBufferSystem;

    private struct SelectorData
    {
        public float3 translation;
        public float rangeSquared;
        public SelectorID id;
    }


    [BurstCompile]
    private struct SelectorTranslationsJob : IJobForEachWithEntity<SelectorComponent, Translation>
    {
        public NativeArray<SelectorData> selectorDatas;

        public void Execute(Entity entity, int index, [ReadOnly] ref SelectorComponent selector, [ReadOnly] ref Translation translation)
        {
#if UNITY_EDITOR
            if (selector.id == SelectorID.Undefined)
            {
                throw new Exception($"Selector has ID set to Undefined");
            }
#endif
            selectorDatas[index] = new SelectorData()
            {
                translation = translation.Value,
                rangeSquared = selector.range * selector.range,
                id = selector.id,
            };
        }

    }


    [BurstCompile]
    private struct UpdateSelectablesJob : IJobForEach<SelectableComponent, Translation>
    {
        [ReadOnly] public int selectorCount;
        [ReadOnly] public NativeArray<SelectorData> selectorDatas;

        public void Execute(ref SelectableComponent selectable, [ReadOnly] ref Translation translation)
        {
            float closestSelectorDistanceSquared = float.PositiveInfinity;
            int closestSelectorIndex = -1;
            for (int i = 0; i < selectorCount; i++)
            {
                float distanceSquared = math.lengthsq(translation.Value - selectorDatas[i].translation);
                if (distanceSquared <= selectorDatas[i].rangeSquared)
                {
                    if (distanceSquared < closestSelectorDistanceSquared)
                    {
                        closestSelectorDistanceSquared = distanceSquared;
                        closestSelectorIndex = i;
                    }
                }
            }

            if (closestSelectorIndex == -1)
            {
                selectable.selectorId = SelectorID.Undefined;
            }
            else
            {
                selectable.selectorId = selectorDatas[closestSelectorIndex].id;
                selectable.selectorTranslation = selectorDatas[closestSelectorIndex].translation;
            }
        }
    }

    private struct UpdateSelectableGroupsJob : IJobForEachWithEntity<SelectableComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public void Execute(Entity entity, int index, ref SelectableComponent selectable)
        {
            commandBuffer.SetSharedComponent(index, entity, new SelectableGroupComponent()
            {
                id = selectable.selectorId,
            });
        }
    }

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

        selectorQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<SelectorComponent>(),
                ComponentType.ReadOnly<Translation>(),
            }
        });

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int selectorCount = selectorQuery.CalculateEntityCount();
        if (selectorCount == 0)
        {
            return inputDeps;
        }

        NativeArray<SelectorData> selectorDatas = new NativeArray<SelectorData>(selectorCount, Allocator.TempJob);
        NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(selectorCount, Allocator.TempJob);

        JobHandle gatherSelectorDatasHandle = new SelectorTranslationsJob()
        {
            selectorDatas = selectorDatas,
        }.Schedule(selectorQuery, inputDeps);


        JobHandle gatherUnselectedHandle = new UpdateSelectablesJob()
        {
            selectorDatas = selectorDatas,
            selectorCount = selectorCount,
        }.Schedule(this, gatherSelectorDatasHandle);

        for (int i = 0; i < selectorCount; i++)
        {
            jobHandles[i] = new UpdateSelectableGroupsJob()
            {
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            }.Schedule(this, gatherUnselectedHandle);
        }

        JobHandle combineHandle = JobHandle.CombineDependencies(jobHandles);
        JobHandle selectorDatasDispose = selectorDatas.Dispose(combineHandle);
        JobHandle jobHandlesDispose = jobHandles.Dispose(combineHandle);
        return JobHandle.CombineDependencies(selectorDatasDispose, jobHandlesDispose);
    }
}
