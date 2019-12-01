using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SelectorSpawnerSystem : JobComponentSystem
{
    private EndInitializationEntityCommandBufferSystem commandBufferSystem;
    private EntityQuery spawnQuery;
    private EntityQuery despawnQuery;

    private struct DestroySelectorJob : IJobForEachWithEntity<SelectorComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, ref SelectorComponent selector)
        {
            commandBuffer.DestroyEntity(index, entity);
        }
    }
    private struct SpawnSelectorJob : IJobForEachWithEntity<SelectorSpawnerComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float3 mousePosition;

        public void Execute(Entity entity, int index, ref SelectorSpawnerComponent selectorSpawner)
        {
            Entity selector = commandBuffer.Instantiate(index, selectorSpawner.selectorPrefab);
            commandBuffer.SetComponent(index, selector, new SelectorComponent
            {
                mode = selectorSpawner.mode,
                range = selectorSpawner.range,
            });
            commandBuffer.AddSharedComponent(index, selector, new SelectableGroupComponent
            {
                mode = selectorSpawner.mode,
            });
            commandBuffer.SetComponent(index, selector, new Translation
            {
                Value = mousePosition,
            });
        }
    }

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        despawnQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<SelectorComponent>(),
                typeof(SelectableGroupComponent),
            }
        });

        spawnQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<SelectorSpawnerComponent>(),
                typeof(SelectableGroupComponent),
            }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        bool leftDown = Input.GetKeyDown(KeyCode.Mouse0) == true;
        bool leftUp = Input.GetKeyUp(KeyCode.Mouse0) == true;
        bool rightDown = Input.GetKeyDown(KeyCode.Mouse1) == true;
        bool rightUp = Input.GetKeyUp(KeyCode.Mouse1) == true;

        if (leftDown == true || rightDown == true)
        {
            spawnQuery.ResetFilter();
            if (leftDown == false || rightDown == false)
            {
                spawnQuery.AddSharedComponentFilter(new SelectableGroupComponent
                {
                    mode = leftDown == true ? SelectorMode.Attract : SelectorMode.Repel,
                });
            }
            inputDeps = new SpawnSelectorJob
            {
                mousePosition = PlayerInput.Singleton.MouseHitPosition,
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            }.Schedule(spawnQuery, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

        }

        if (leftUp == true || rightUp == true)
        {
            despawnQuery.ResetFilter();
            if (leftUp == false || rightUp == false)
            {
                despawnQuery.AddSharedComponentFilter(new SelectableGroupComponent
                {
                    mode = leftUp == true ? SelectorMode.Attract : SelectorMode.Repel,
                });
            }
            inputDeps = new DestroySelectorJob
            {
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            }.Schedule(despawnQuery, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);
        }

        return inputDeps;

    }
}