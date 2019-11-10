
using System;
using Unity.Entities;

[Serializable]
public struct AnimationCurveBlob
{
    public BlobArray<float> samples;
}
