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
        [Layer]
        public int HandLayer = -1;
        
        private GestureRecognizer _gestureRecognizer = null;

       // private GRPLTeleport _teleporter = null;

       private void OnValidate()
       {
           Assert.IsTrue(HandLayer is >= 0 and <= 31, "GrappleManager, Hand layer is not valid. Layer value should be between 0 and 31.");
       }

       void Start()
        {
            _jointManager = gameObject.AddComponent<JointManager>();
            _jointManager.HandLayer = HandLayer;
            _gestureRecognizer = GetComponent<GestureRecognizer>();
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
                Debug.LogError($"{nameof(GrappleManager)} Failed to add {SelectedPhysicsService} service");
            }

            //if (gameObject.TryGetComponent(out _teleporter))
            //    _teleporter.Initialize(_jointManager, _gestureRecognizer);
            //else
            //{
            //    Debug.LogError($"{nameof(HandInputManager)} Failed to find {nameof(GRPLTeleport)}");
            //    return;
            //}

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