namespace Rhinox.XR.Grapple.It
{
    public interface IPhysicsService
    {
        void Initialize(GRPLJointManager jointManager);
        bool IsInitialized();
        void Update();
        void SetHandEnabled(bool newState, RhinoxHand handedness);
        bool IsHandEnabled(RhinoxHand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.Initialize(GRPLJointManager jointManager) { }

        bool IPhysicsService.IsInitialized()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, RhinoxHand handedness) { }

        bool IPhysicsService.IsHandEnabled(RhinoxHand handedness)
        { return false; }
    }
}