
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class BoidHelper
{

    const int numViewDirections = 300;
    public static readonly BlobAssetReference<BlobArray<float3>> directions;

    static BoidHelper()
    {
        using (BlobBuilder builder = new BlobBuilder(Allocator.Temp))
        {
            ref BlobArray<float3> blob = ref builder.ConstructRoot<BlobArray<float3>>();
            BlobBuilderArray<float3> directions = builder.Allocate(ref blob, numViewDirections);

            float goldenRatio = (1 + math.sqrt(5)) / 2;
            float angleIncrement = math.PI * 2 * goldenRatio;

            for (int i = 0; i < numViewDirections; i++)
            {
                float t = (float)i / numViewDirections;
                float inclination = math.acos(1 - 2 * t);
                float azimuth = angleIncrement * i;

                float x = math.sin(inclination) * math.cos(azimuth);
                //float y = math.sin(inclination) * math.sin(azimuth);
                float z = math.cos(inclination);
                directions[i] = new float3(x, 0, z);
            }
            BoidHelper.directions = builder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);
        }
    }

}