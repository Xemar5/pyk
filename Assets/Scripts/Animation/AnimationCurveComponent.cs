using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct AnimationCurveTranslation : IComponentData
{

    public BlobAssetReference<AnimationCurveBlob> xCurve;
    public BlobAssetReference<AnimationCurveBlob> yCurve;
    public BlobAssetReference<AnimationCurveBlob> zCurve;
    public int frameDelay;
    public float fps;

}
