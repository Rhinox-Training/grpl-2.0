using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    public class PhysicsSocketService : BasePhysicsService
    {
        private bool _isInitialized = false;
        public override void Initialize(BoneManager boneManager)
        {
            throw new System.NotImplementedException();
        }

        public override bool GetIsInitialised()
        {
            return _isInitialized;
        }

        public override void SetEnabled(bool newState, Hand handedness)
        {
            throw new System.NotImplementedException();
        }

        public override bool GetIsEnabled(Hand handedness)
        {
            throw new System.NotImplementedException();
        }

        public override void ManualUpdate()
        {
            throw new System.NotImplementedException();
        }

        public override void SetHandLayer(LayerMask layer)
        {
            throw new System.NotImplementedException();
        }
    }
}