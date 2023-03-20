using Rhinox.GUIUtils.Attributes;
using UnityEditor.SceneManagement;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Assertions;

namespace Rhinox.XR.Grapple.It
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class GrappleManager : MonoBehaviour
    {
        private JointManager _jointManager = null;

        private IPhysicsService _physicsService = new NullPhysicsService();

        [Header("Physics")]
        public PhysicServices SelectedPhysicsService = PhysicServices.None;
        [Layer][SerializeField] private int _handLayer = 2;
        public int Handlayer => _handLayer;

        private GestureRecognizer _gestureRecognizer = null;

        private GRPLTeleport _teleporter = null;

        void Start()
        {
            _jointManager = gameObject.AddComponent<JointManager>();
            _jointManager.HandLayer = _handLayer;
            _gestureRecognizer = GetComponent<GestureRecognizer>();
            _gestureRecognizer.Initialize(_jointManager);

            switch (SelectedPhysicsService)
            {
                case PhysicServices.Socketing:
                    {
                        _physicsService = new PhysicsSocketService(_jointManager, _gestureRecognizer, gameObject);
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
                Debug.LogError($"{nameof(GrappleManager)} Failed to add {SelectedPhysicsService} service");
            }

            if (gameObject.TryGetComponent(out _teleporter))
                _teleporter.Initialize(_jointManager, _gestureRecognizer);
            else
            {
                Debug.LogError($"{nameof(GrappleManager)} Failed to find {nameof(GRPLTeleport)}");
                return;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!_physicsService.IsInitialized())
            {
                _physicsService.TryInitialize();
            }

            _physicsService.Update();
        }
    }
}