using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public static class AnimationCurveExtention
{
    public struct AnimationCurveSampled
    {
        public BlobArray<float> samples;
    }

    public static AnimationCurveSampled PresampleCurveToArray(this AnimationCurve animationCurve, int resolution)
    {
        using (BlobBuilder builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
        {
            ref AnimationCurveSampled blobArray = ref builder.ConstructRoot<AnimationCurveSampled>();
            builder.Allocate(ref blobArray.samples, resolution);
            for (int i = 0; i < resolution; i++)
            {
                blobArray.samples[i] = animationCurve.Evaluate(i / (resolution - 1));
            }
            //BlobAssetReference<AnimationCurveSampled> reference = builder.CreateBlobAssetReference<AnimationCurveSampled>(Unity.Collections.Allocator.Persistent);
            return blobArray;
        }
    }
}
