using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;


[DisallowMultipleComponent]
[RequiresEntityConversion]
public class SelectableComponentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<SelectableComponent>(entity);
        dstManager.AddSharedComponentData(entity, new SelectableGroupComponent()
        {
            id = SelectorID.Undefined
        });
    }
}