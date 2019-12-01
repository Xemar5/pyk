using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

public enum MouseButton
{
    Left,
    Right,
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class SelectorSpawnerComponentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    private GameObject selectorPrefab = null;
    [SerializeField]
    private MouseButton mouseButton = MouseButton.Left;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SelectorSpawnerComponent()
        {
            selectorPrefab = conversionSystem.GetPrimaryEntity(selectorPrefab),
            mouseButton = mouseButton,
        });
    }
}
