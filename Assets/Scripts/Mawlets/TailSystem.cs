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

public class TailSystem : JobComponentSystem
{
    private EntityQuery tailQuery;

    private struct TailNode
    {
        public float3 translation;
        public quaternion rotation;
        public Entity entity;
    }

    [BurstCompile]
    private struct GetTargetTranslations : IJobForEachWithEntity<Translation, Rotation>
    {
        public NativeArray<TailNode> translations;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Rotation rotation)
        {
            translations[index] = new TailNode()
            {
                translation = translation.Value,
                rotation = rotation.Value,
                entity = entity
            };
        }
    }

    [BurstCompile]
    private struct MoveTailJob : IJobForEachWithEntity<TailComponent, Translation, Rotation>
    {
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<TailNode> translations;

        public void Execute(Entity entity, int index, ref TailComponent tail, ref Translation translation, ref Rotation rotation)
        {
            if (tail.target == Entity.Null)
            {
                return;
            }
            for (int i = 0; i < translations.Length; i++)
            {
                if (translations[i].entity == tail.target)
                {
                    translation.Value = math.lerp(translation.Value, translations[i].translation - math.mul(translations[i].rotation, tail.targetOffset), 0.1f);
                    translation.Value = translations[i].translation + math.normalizesafe(translation.Value - translations[i].translation) * math.length(tail.targetOffset);
                    rotation.Value = quaternion.LookRotation(translations[i].translation - translation.Value, new float3(0, 1, 0));
                }
            }
        }
    }

    protected override void OnCreate()
    {
        tailQuery = base.GetEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadWrite<TailComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
            }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int count = tailQuery.CalculateEntityCount();

        NativeArray<TailNode> translations = new NativeArray<TailNode>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var getTranslations = new GetTargetTranslations()
        {
            translations = translations,
        }.Schedule(tailQuery, inputDeps);

        var moveTail = new MoveTailJob()
        {
            translations = translations,
        }.Schedule(tailQuery, getTranslations);

        return moveTail;
    }


}