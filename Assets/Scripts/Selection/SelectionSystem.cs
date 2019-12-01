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
        public SelectorMode id;
    }


    [BurstCompile]
    private struct SelectorTranslationsJob : IJobForEachWithEntity<SelectorComponent, Translation>
    {
        public NativeArray<SelectorData> selectorDatas;

        public void Execute(Entity entity, int index, [ReadOnly] ref SelectorComponent selector, [ReadOnly] ref Translation translation)
        {
#if UNITY_EDITOR
            if (selector.mode == SelectorMode.Undefined)
            {
                throw new Exception($"Selector has ID set to Undefined");
            }
#endif
            selectorDatas[index] = new SelectorData()
            {
                translation = translation.Value,
                rangeSquared = selector.range * selector.range,
                id = selector.mode,
            };
        }

    }


    //[BurstCompile]
    private struct UpdateSelectablesJob : IJobForEachWithEntity<SelectableComponent, Translation>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        [ReadOnly] public int selectorCount;
        [ReadOnly] public NativeArray<SelectorData> selectorDatas;

        public void Execute(Entity entity, int index, ref SelectableComponent selectable, [ReadOnly] ref Translation translation)
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

            SelectorMode previousMode = selectable.selectorMode;
            if (closestSelectorIndex == -1)
            {
                selectable.selectorMode = SelectorMode.Undefined;
            }
            else
            {
                selectable.selectorMode = selectorDatas[closestSelectorIndex].id;
                selectable.selectorTranslation = selectorDatas[closestSelectorIndex].translation;
            }
            if (previousMode != selectable.selectorMode)
            {
                commandBuffer.SetSharedComponent(index, entity, new SelectableGroupComponent()
                {
                    mode = selectable.selectorMode,
                });
            }
        }
    }

    private struct ClearSelectionJob : IJobForEachWithEntity<SelectableComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, ref SelectableComponent c0)
        {
            if (c0.selectorMode != SelectorMode.Undefined)
            {
                c0.selectorMode = SelectorMode.Undefined;
                commandBuffer.SetSharedComponent(index, entity, new SelectableGroupComponent
                {
                    mode = SelectorMode.Undefined,
                });
            }
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
            return new ClearSelectionJob()
            {
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            }.Schedule(this, inputDeps);
        }

        NativeArray<SelectorData> selectorDatas = new NativeArray<SelectorData>(selectorCount, Allocator.TempJob);

        JobHandle gatherSelectorDatasHandle = new SelectorTranslationsJob()
        {
            selectorDatas = selectorDatas,
        }.Schedule(selectorQuery, inputDeps);


        JobHandle gatherUnselectedHandle = new UpdateSelectablesJob()
        {
            selectorDatas = selectorDatas,
            selectorCount = selectorCount,
            commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        }.Schedule(this, gatherSelectorDatasHandle);

        selectorDatas.Dispose(gatherUnselectedHandle);
        return gatherUnselectedHandle;
    }
}
