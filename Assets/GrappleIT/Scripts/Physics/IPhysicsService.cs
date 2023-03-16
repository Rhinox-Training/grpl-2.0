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
        void TryInitialize();
        bool GetIsInitialised();
        void Update();
        void SetHandEnabled(bool newState, RhinoxHand handedness);
        bool GetIsHandEnabled(RhinoxHand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.TryInitialize() { }

        bool IPhysicsService.GetIsInitialised()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, RhinoxHand handedness) { }

        bool IPhysicsService.GetIsHandEnabled(RhinoxHand handedness)
        { return false; }
    }
}