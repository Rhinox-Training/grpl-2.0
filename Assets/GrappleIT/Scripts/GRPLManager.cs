using Rhinox.GUIUtils.Attributes;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    [RequireComponent(typeof(GRPLGestureRecognizer))]
    public class GRPLManager : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private bool _enableSocketing = true;
        [Layer][SerializeField] private int _handLayer = 2;
        public int Handlayer => _handLayer;


        private GRPLJointManager _jointManager = null;
        private IPhysicsService _physicsService = new NullPhysicsService();
        private GRPLGestureRecognizer _gestureRecognizer = null;
        private GRPLTeleport _teleporter = null;

        private void Start()
        {
            _jointManager = gameObject.AddComponent<GRPLJointManager>();
            _jointManager.HandLayer = _handLayer;
            _gestureRecognizer = GetComponent<GRPLGestureRecognizer>();

            if (_enableSocketing)
                _physicsService = new GRPLPhysicsSocketService(_gestureRecognizer, gameObject);

            if (_enableSocketing && _physicsService.GetType() == typeof(NullPhysicsService))
                PLog.Error<GRPLITLogger>($"{nameof(GRPLManager)} Failed to add Socketing service", this);


            if (gameObject.TryGetComponent(out _teleporter))
                _teleporter.Initialize(_jointManager, _gestureRecognizer);
            else
            {
                PLog.Warn<GRPLITLogger>($"{nameof(GRPLManager)} Failed to find {nameof(GRPLTeleport)}", this);
                return;
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (_physicsService.IsInitialized())
            {
                _physicsService.Update();
            }
        }
    }
}