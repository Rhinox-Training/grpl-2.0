using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class HandInputManager : MonoBehaviour
    {
        private JointManager _jointManager = null;

        private IPhysicsService _physicsService = new NullPhysicsService();

        [Header("Physics")]
        public PhysicServices SelectedPhysicsService = PhysicServices.None;

        private GestureRecognizer _gestureRecognizer = null;

        private GRPLTeleport _teleporter = null;

        void Start()
        {
            _jointManager = gameObject.AddComponent<JointManager>();
            if (_jointManager == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to add {nameof(JointManager)}");
                return;
            }

            _gestureRecognizer = GetComponent<GestureRecognizer>();
            if (_gestureRecognizer == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to find {nameof(GestureRecognizer)}");
                return;
            }
            _gestureRecognizer.Initialize(_jointManager);

            switch (SelectedPhysicsService)
            {
                case PhysicServices.Socketing:
                    {
                        _physicsService = new PhysicsSocketService(_jointManager, gameObject);
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

            if (gameObject.TryGetComponent(out _teleporter))
                _teleporter.Initialize(_jointManager, _gestureRecognizer);
            else
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to find {nameof(GRPLTeleport)}");
                return;
            }

            //_teleporter = gameObject.GetComponent<GRPLTeleport>();//gameObject.AddComponent<GRPLTeleport>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!_physicsService.GetIsInitialised())
            {
                _physicsService.TryInitialize();
            }

            _physicsService.Update();
        }
    }
}