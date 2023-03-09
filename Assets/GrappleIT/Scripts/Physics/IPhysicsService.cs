using System;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public enum PhysicServices
    {
        None,
        Socketing
    }

    public interface IPhysicsService
    {
        void TryInitialize();
        bool GetIsInitialised();
        void Update();
        void SetEnabled(bool newState, Hand handedness);
        bool GetIsEnabled(Hand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.TryInitialize() { }

        bool IPhysicsService.GetIsInitialised()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetEnabled(bool newState, Hand handedness) { }

        bool IPhysicsService.GetIsEnabled(Hand handedness)
        { return false; }
    }
}