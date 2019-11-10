using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


public static class AnimationCurveExtention
{

    public static BlobAssetReference<AnimationCurveBlob> PresampleCurveToArray(this AnimationCurve animationCurve, float fps)
    {
        using (BlobBuilder builder = new BlobBuilder(Allocator.Temp))
        {
            float duration = animationCurve.keys[animationCurve.length - 1].time;
            int resolution = (int)(fps * duration);
            ref AnimationCurveBlob blobArray = ref builder.ConstructRoot<AnimationCurveBlob>();
            BlobBuilderArray<float> array = builder.Allocate(ref blobArray.samples, resolution);
            for (int i = 0; i < resolution; i++)
            {
                array[i] = animationCurve.Evaluate((float)i / (float)(resolution - 1) * duration);
            }
            return builder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);
        }
    }
}
