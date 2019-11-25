using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct AttackerComponent : IComponentData
{
    public int damage;
    public float attackDelay;
}