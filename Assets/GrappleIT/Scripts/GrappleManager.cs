using Rhinox.GUIUtils.Attributes;
using UnityEngine;

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

        // private GRPLTeleport _teleporter = null;

        private void Start()
        {
            _jointManager = gameObject.AddComponent<JointManager>();
            _jointManager.HandLayer = _handLayer;
            _gestureRecognizer = GetComponent<GestureRecognizer>();

            switch (SelectedPhysicsService)
            {
                case PhysicServices.Socketing:
                    {
                        _physicsService = new PhysicsSocketService(_gestureRecognizer, gameObject);
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
        private void Update()
        {
            if (_physicsService.GetIsInitialised())
            {
                _physicsService.Update();
            }

        }
    }
}