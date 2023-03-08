using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;


namespace Rhinox.XR.Grapple.It
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class HandInputManager : MonoBehaviour
    {
        private BoneManager _boneManager = null;

        private IPhysicsService _physicsService = new NullPhysicsService();

        [Header("Physics")]
        public PhysicServices SelectedPhysicsService = PhysicServices.None;


        private Vector3 HandSocketPositionalOffset = Vector3.zero;
        private Quaternion HandSocketRotationalOffset = Quaternion.identity;


        private GestureRecognizer _gestureRecognizer = null;


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
                        var newService = new PhysicsSocketService(_boneManager, gameObject, HandSocketPositionalOffset, HandSocketRotationalOffset);
                        _physicsService = newService;
                        //newService.Initialize();
                        //if (!newService.GetIsInitialised())
                        //    _physicsService = null;
                        //else
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
            _gestureRecognizer.Initialize(_boneManager);

        }
    }
}
