namespace Rhinox.XR.Grapple.It
{
    public enum PhysicServices
    {
        None,
        Socketing
    }

    public interface IPhysicsService
    {
        void Initialize(JointManager jointManager);
        bool GetIsInitialised();
        void Update();
        void SetHandEnabled(bool newState, RhinoxHand handedness);
        bool GetIsHandEnabled(RhinoxHand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.Initialize(JointManager jointManager) { }

        bool IPhysicsService.GetIsInitialised()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, RhinoxHand handedness) { }

        bool IPhysicsService.GetIsHandEnabled(RhinoxHand handedness)
        { return false; }
    }
}