using System;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public enum PhysicServices
    {
        None,
        Socketing,
        KinematicProxy
    }

    public interface IPhysicsService
    {
        void TryInitialize();
        bool GetIsInitialised();
        void Update();
        void SetHandEnabled(bool newState, Hand handedness);
        bool GetIsHandEnabled(Hand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.TryInitialize() { }

        bool IPhysicsService.GetIsInitialised()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, Hand handedness) { }

        bool IPhysicsService.GetIsHandEnabled(Hand handedness)
        { return false; }
    }
}