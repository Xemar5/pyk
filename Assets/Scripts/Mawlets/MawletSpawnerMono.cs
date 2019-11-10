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
        for(int i= 0; i< 1; i++)
        {
            SpawnMawlets(1, new float3(UnityEngine.Random.Range(-20,20), 0, UnityEngine.Random.Range(-20, 20)), mawletPrefab);
        }

    }


    public void SpawnMawlets(int count, float3 translation, GameObject prefab)
    {
        Entity convertedPrefab = GetConvertedEntity(prefab);
        for (int i = 0; i < count; i++)
        {
            Entity entity = entityManager.Instantiate(convertedPrefab);
            entityManager.SetComponentData(entity, new Translation { Value = translation });
            entityManager.SetComponentData(entity, new Rotation { Value = Quaternion.identity });
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
