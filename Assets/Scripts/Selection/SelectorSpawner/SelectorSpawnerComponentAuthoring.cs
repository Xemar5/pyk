using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class SelectorSpawnerComponentAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField]
    private GameObject selectorPrefab = null;
    [SerializeField]
    private SelectorMode mode = SelectorMode.Undefined;
    [SerializeField]
    private float range = 25;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(selectorPrefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SelectorSpawnerComponent()
        {
            selectorPrefab = conversionSystem.GetPrimaryEntity(selectorPrefab),
            range = range,
            mode = mode,
        });
        dstManager.AddSharedComponentData(entity, new SelectableGroupComponent()
        {
            mode = mode,
        });
    }

}
