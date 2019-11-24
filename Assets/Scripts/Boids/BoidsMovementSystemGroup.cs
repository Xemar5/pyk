using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;

//[UpdateAfter(typeof(BuildPhysicsWorld))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BoidsMovementSystemGroup : ComponentSystemGroup
{
    //private BoidsMovementDataUpdateSystem boidsMovementDataUpdateSystem;
    //private BoidsMovementSystem boidsMovementSystem;
    //private BoidsPositionClampSystem boidsPositionClampSystem;
    //private UncontrolledMovementSystem boidsUncontrolledMovementSystem;

    //protected override void OnCreate()
    //{
    //    this.boidsMovementDataUpdateSystem = World.GetOrCreateSystem<BoidsMovementDataUpdateSystem>();
    //    this.boidsMovementSystem = World.GetOrCreateSystem<BoidsMovementSystem>();
    //    this.boidsPositionClampSystem = World.GetOrCreateSystem<BoidsPositionClampSystem>();
    //    this.boidsUncontrolledMovementSystem = World.GetOrCreateSystem<UncontrolledMovementSystem>();
    //}

    //protected override void OnUpdate()
    //{
    //    boidsMovementDataUpdateSystem.Update();
    //    boidsUncontrolledMovementSystem.Update();
    //    boidsMovementSystem.Update();
    //    boidsPositionClampSystem.Update();
    //}
}