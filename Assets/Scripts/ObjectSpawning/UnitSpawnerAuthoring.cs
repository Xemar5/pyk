using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class UnitSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField]
    private GameObject headPrefab = null;
    [SerializeField]
    private GameObject tailPrefab = null;
    [SerializeField]
    private int tailLength = 3;
    [SerializeField]
    private Vector3 tailOffset = Vector3.zero;
    [SerializeField]
    private int spawnCount = 1;


    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(headPrefab);
        referencedPrefabs.Add(tailPrefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        UnitSpawnerComponent unitSpawnerComponent = new UnitSpawnerComponent()
        {
            headPrefab = conversionSystem.GetPrimaryEntity(headPrefab),
            tailPrefab = conversionSystem.GetPrimaryEntity(tailPrefab),
            tailLength = tailLength,
            tailOffset = tailOffset,
            spawnCount = spawnCount,
            translation = transform.position,
            rotation = transform.rotation,
        };
        dstManager.AddComponentData(entity, unitSpawnerComponent);
    }

}
