namespace Rhinox.XR.Grapple.It
{
    public enum PhysicServices
    {
        None,
        Socketing,
        KinematicProxy
    }

    public interface IPhysicsService
    {
        void Initialize(JointManager jointManager);
        bool IsInitialized();
        void Update();
        void SetHandEnabled(bool newState, RhinoxHand handedness);
        bool IsHandEnabled(RhinoxHand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.Initialize(JointManager jointManager) { }

        bool IPhysicsService.IsInitialized()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, RhinoxHand handedness) { }

        bool IPhysicsService.IsHandEnabled(RhinoxHand handedness)
        { return false; }
    }
}