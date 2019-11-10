using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class AnimationCurveAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    private AnimationCurve xCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
    [SerializeField]
    private AnimationCurve yCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
    [SerializeField]
    private AnimationCurve zCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
    [SerializeField]
    private float fps = 60;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimationCurveTranslation
        {
            xCurve = xCurve.PresampleCurveToArray(fps),
            yCurve = yCurve.PresampleCurveToArray(fps),
            zCurve = zCurve.PresampleCurveToArray(fps),
            frameDelay = -1,
            fps = fps,
        });
    }
}
