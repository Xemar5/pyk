using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoidSettings", menuName = "BoidSettings")]
public class BoidsSettingsData : ScriptableObject
{
    public BoidsSettings settings;

}

[System.Serializable]
public struct BoidsSettings
{
    public float maxSpeed;
    public float minSpeed;
    public float maxSteerForce;
    public float viewRadius;
    public float avoidRadius;
    public float alignWeight;
    public float separationWeight;
    public float cohesionWeight;
    public float targetWeight;
}