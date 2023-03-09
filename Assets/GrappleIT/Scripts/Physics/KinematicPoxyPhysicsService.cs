using Rhinox.XR.Grapple;
using UnityEngine;
using UnityEngine.XR.Hands;


public class KinematicPoxyPhysicsService : IPhysicsService
{
    /// <summary>
    /// This object creates the necessary components for the proxy and links it to the given bone.
    /// </summary>
    private class KinematicProxyObject
    {
        private Hand _handedness;
        private RhinoxJoint _joint;
        private CapsuleCollider _collider;
        private Rigidbody _rigidbody;


        public KinematicProxyObject()
        {
            
        }
        

        private void Initialize()
        {
            
        }
    }
    

    private JointManager _jointManager;

    
    
    
    private bool _isInitialized = false;

    public KinematicPoxyPhysicsService(JointManager jointManager)
    {
        _jointManager = jointManager;
    }

    public void TryInitialize()
    {
        _isInitialized = true;
    }

    private void TryGenerateCapsules(Handedness hand)
    {
        
    }
    
    
    public bool GetIsInitialised()
    {
        return _isInitialized;
    }

    void IPhysicsService.Update()
    {
        Update();
    }

    public void SetEnabled(bool newState, Hand handedness)
    {
        throw new System.NotImplementedException();
    }

    public bool GetIsEnabled(Hand handedness)
    {
        throw new System.NotImplementedException();
    }

    void Update()
    {
        
    }
}
