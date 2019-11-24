
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class BoidHelper
{

    const int numViewDirections = 60;
    public static readonly BlobAssetReference<BlobArray<float3>> directions;
    public static readonly BoidsSettings boidSettings;

    static BoidHelper()
    {
        boidSettings = Resources.Load<BoidsSettingsData>("BoidSettings").settings;
        using (BlobBuilder builder = new BlobBuilder(Allocator.Temp))
        {
            ref BlobArray<float3> blob = ref builder.ConstructRoot<BlobArray<float3>>();
            BlobBuilderArray<float3> directions = builder.Allocate(ref blob, numViewDirections);

            for (int i = 0; i < numViewDirections; i++)
            {
                int k;
                if (i % 2 == 0)
                {
                    k = i / 2;
                }
                else
                {
                    k = numViewDirections - (i + 1) / 2;
                }
                float t = (float)k / (float)numViewDirections;
                float angle = t * math.PI * 2 + math.PI / 2f;

                float x = math.cos(angle);
                float z = math.sin(angle);
                directions[i] = new float3(x, 0, z);
            }

            //float goldenRatio = (1 + math.sqrt(5)) / 2;
            //float angleIncrement = math.PI * 2 * goldenRatio;

            //for (int i = 0; i < numViewDirections; i++)
            //{
            //    float t = (float)i / numViewDirections;
            //    float inclination = math.acos(1 - 2 * t);
            //    float azimuth = angleIncrement * i;

            //    float x = math.sin(inclination) * math.cos(azimuth);
            //    //float y = math.sin(inclination) * math.sin(azimuth);
            //    float z = math.cos(inclination);
            //    directions[i] = new float3(x, 0, z);
            //}
            BoidHelper.directions = builder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);
        }
    }

}