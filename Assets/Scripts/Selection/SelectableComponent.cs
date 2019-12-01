using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SelectableComponent : IComponentData
{
    public SelectorMode selectorMode;
    public float3 selectorTranslation;
}