using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct DamageComponent : IComponentData
{
    public int damage;
    public Entity attacker;
}
