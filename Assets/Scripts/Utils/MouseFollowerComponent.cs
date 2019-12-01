using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct MouseFollowerComponent : IComponentData
{
    [Range(0, 1)]
    public float weight;
}
