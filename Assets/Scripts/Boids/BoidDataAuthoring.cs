using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BoidDataAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    // Add fields to your component here. Remember that:
    //
    // * The purpose of this class is to store data for authoring purposes - it is not for use while the game is
    //   running.
    // 
    // * Traditional Unity serialization rules apply: fields must be public or marked with [SerializeField], and
    //   must be one of the supported types.
    //
    // For example,
    //    public float scale;
    [SerializeField]
    float movementSpeed,viewRadius,avoidRadius, maxSpeed, maxSteerForce;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Call methods on 'dstManager' to create runtime components on 'entity' here. Remember that:
        //
        // * You can add more than one component to the entity. It's also OK to not add any at all.
        //
        // * If you want to create more than one entity from the data in this class, use the 'conversionSystem'
        //   to do it, instead of adding entities through 'dstManager' directly.
        //
        // For example,
        dstManager.AddComponentData(entity, new BoidData { 
            movementSpeed = movementSpeed , 
            viewRadius = viewRadius, 
            avoidRadius = avoidRadius,
            maxSpeed = maxSpeed,
            maxSteerForce = maxSteerForce,
            velocity = new float3(UnityEngine.Random.Range(-1,1),0,UnityEngine.Random.Range(-1,1))
        });
        
        
    }
}
