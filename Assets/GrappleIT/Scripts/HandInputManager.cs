using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;


namespace Rhinox.XR.Grapple
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class HandInputManager : MonoBehaviour
    {
        private JointManager _jointManager = null;

        private IPhysicsService _physicsService = new NullPhysicsService();

        [Header("Physics")]
        public PhysicServices SelectedPhysicsService = PhysicServices.None;

        private GestureRecognizer _gestureRecognizer = null;
        
        
        void Start()
        {
            _jointManager = gameObject.AddComponent<JointManager>();
            if (_jointManager == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to add {nameof(JointManager)}");
                return;
            }

            switch (SelectedPhysicsService)
            {
                case PhysicServices.Socketing:
                    {
                        var newService = new PhysicsSocketService(_jointManager, gameObject);
                        _physicsService = newService;
                        //newService.Initialize();
                        //if (!newService.GetIsInitialised())
                        //    _physicsService = null;
                        //else
                        break;
                    }
                case PhysicServices.KinematicProxy:
                {
                    _physicsService = new KinematicPoxyPhysicsService(_jointManager);
                    break;
                }
            }

            if (_physicsService.GetType() == typeof(NullPhysicsService))
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to add {SelectedPhysicsService} service");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!_physicsService.GetIsInitialised())
            {
                _physicsService.TryInitialize();
            }

            _physicsService.Update();
            
            _gestureRecognizer = GetComponent<GestureRecognizer>();
            _gestureRecognizer.Initialize(_jointManager);

        }
    }
}
