﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct DefenderComponent : IComponentData
{
    public int health;
}