using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct BoidData : IComponentData
{
    // Add fields to your component here. Remember that:
    //
    // * A component itself is for storing data and doesn't 'do' anything.
    //
    // * To act on the data, you will need a System.
    //
    // * Data in a component must be blittable, which means a component can
    //   only contain fields which are primitive types or other blittable
    //   structs; they cannot contain references to classes.
    //
    // * You should focus on the data structure that makes the most sense
    //   for runtime use here. Authoring Components will be used for 
    //   authoring the data in the Editor.

    public float movementSpeed;
    public float maxSpeed;
    public float maxSteerForce;
    public float viewRadius;
    public float avoidRadius;
    public int numFlockmates;
    public float3 velocity;
    public float3 flockHeading;
    public float3 flockCentre;
    public float3 avoidanceHeading;
}
