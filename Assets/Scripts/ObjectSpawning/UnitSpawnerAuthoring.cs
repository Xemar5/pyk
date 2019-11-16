using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class UnitSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField]
    private GameObject prefab = null;
    [SerializeField]
    private int spawnCount = 1;


    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(prefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        UnitSpawnerComponent unitSpawnerComponent = new UnitSpawnerComponent()
        {
            prefab = conversionSystem.GetPrimaryEntity(prefab),
            spawnCount = spawnCount,
            translation = transform.position,
        };
        dstManager.AddComponentData(entity, unitSpawnerComponent);
    }

}
