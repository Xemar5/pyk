using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MawletSpawnerMono : MonoBehaviour
{
    [SerializeField]
    private GameObject mawletPrefab = null;
    private Dictionary<GameObject, Entity> convertedPrefabs = new Dictionary<GameObject, Entity>();
    private EntityManager entityManager;


    private void Start()
    {
        entityManager = World.Active.EntityManager;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnMawlet();
        }
    }

    [ContextMenu("Spawn Mawlet")]
    private void SpawnMawlet()
    {
        SpawnMawlets(1, new float3(0, 0, 0), mawletPrefab);
    }


    public void SpawnMawlets(int count, float3 translation, GameObject prefab)
    {
        Entity convertedPrefab = GetConvertedEntity(prefab);
        for (int i = 0; i < count; i++)
        {
            Entity entity = entityManager.Instantiate(convertedPrefab);
            entityManager.SetComponentData(entity, new Translation { Value = translation });
        }
    }

    private Entity GetConvertedEntity(GameObject prefab)
    {
        if (convertedPrefabs.TryGetValue(prefab, out Entity convertedPrefab) == false)
        {
            convertedPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
        }
        return convertedPrefab;
    }


}
