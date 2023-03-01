using System;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    public interface IPhysicsService
    {
        void Initialize(BoneManager boneManager);
        bool GetIsInitialised();
        void SetEnabled(bool newState, Hand handedness);
        bool GetIsEnabled(Hand handedness);
        void ManualUpdate();
        void SetHandLayer(UnityEngine.LayerMask layer);
    }

    [Serializable]
    public abstract class BasePhysicsService : IPhysicsService
    {
        public abstract void Initialize(BoneManager boneManager);

        public abstract bool GetIsInitialised();

        public abstract void SetEnabled(bool newState, Hand handedness);

        public abstract bool GetIsEnabled(Hand handedness);

        public abstract void ManualUpdate();

        public abstract void SetHandLayer(LayerMask layer);
    }
    
    
}