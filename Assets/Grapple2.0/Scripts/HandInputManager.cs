using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Rhinox.XR.Grapple
{
    public class HandInputManager : MonoBehaviour
    {
        private BoneManager _boneManager = null;

        private IPhysicsService _physicsService = new NullPhysicsService();

        [Header("Physics")]
        public PhysicServices SelectedPhysicsService = PhysicServices.None;

        void Start()
        {
            _boneManager = gameObject.AddComponent<BoneManager>();
            if (_boneManager == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to add {nameof(BoneManager)}");
                return;
            }

            switch (SelectedPhysicsService)
            {
                case PhysicServices.Socketing:
                    {
                        var newService = new PhysicsSocketService(_boneManager, gameObject);
                        if (!newService.GetIsInitialised())
                            _physicsService = null;
                        else
                            _physicsService = newService;
                        break;
                    }
            }

            if (_physicsService == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to add {SelectedPhysicsService} service");
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
