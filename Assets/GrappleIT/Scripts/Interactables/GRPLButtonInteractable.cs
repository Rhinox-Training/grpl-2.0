using System;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLButtonInteractable : GRPLInteractable
    {
        [Header("Poke parameters")] 
        [SerializeField] private Transform _interactableBaseTransform;
        public Transform ButtonBaseTransform => _interactableBaseTransform;
        [SerializeField] private Transform _interactObject;
        public Transform ButtonSurface => _interactObject;

        [Header("Activation parameters")] 
        [SerializeField][Range(0f,1f)] private float _selectStartPercentage = 0.25f;

        public float SelectStartPercentage => _selectStartPercentage;
        private float _maxPressDistance;
        public float MaxPressedDistance => _maxPressDistance;
        
        public event Action OnInteractStarted;
        public event Action OnInteractEnded;
        public event Action OnProximityStarted;
        public event Action OnProximityEnded;

        public Bounds PressBounds { get; private set; }

        protected override void Initialize()
        {
            // Calculate the initial distance between the interact object and base transform
            _maxPressDistance = Vector3.Dot(_interactObject.transform.position - _interactableBaseTransform.position,
                -1f * _interactableBaseTransform.forward);

            var boundExtends = ButtonSurface.gameObject.GetObjectBounds().extents;
            boundExtends.z += 0.005f;

            PressBounds = new Bounds()
            {
                center = ButtonBaseTransform.position,
                extents = boundExtends
            };
        }

        private protected override void InteractStarted()
        {
            OnInteractStarted?.Invoke();
        }

        private protected override void InteractStopped()
        {
            OnInteractEnded?.Invoke();
        }

        private protected override void ProximityStarted()
        {
            PLog.Warn<GrappleItLogger>($"Proximity started with {gameObject.name}");
            OnProximityStarted?.Invoke();
        }

        private protected override void ProximityStopped()
        {
            PLog.Warn<GrappleItLogger>($"Proximity ended with {gameObject.name}");
            OnProximityEnded?.Invoke();

        }
        
        public void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(PressBounds.center, PressBounds.size);
            Gizmos.DrawRay(ButtonBaseTransform.position, -ButtonBaseTransform.forward);
        }
    }
}