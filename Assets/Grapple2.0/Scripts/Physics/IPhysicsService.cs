using System;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    public enum PhysicServices
    {
        None,
        Socketing
    }

    public interface IPhysicsService
    {
        void Initialize();
        bool GetIsInitialised();
        void Update();
        void SetEnabled(bool newState, Hand handedness);
        bool GetIsEnabled(Hand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.Initialize() { }

        bool IPhysicsService.GetIsInitialised()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetEnabled(bool newState, Hand handedness) { }

        bool IPhysicsService.GetIsEnabled(Hand handedness)
        { return false; }
    }
}