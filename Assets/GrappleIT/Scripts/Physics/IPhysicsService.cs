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
        bool IsInitialized();
        void Update();
        void SetHandEnabled(bool newState, Hand handedness);
        bool IsHandEnabled(Hand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.TryInitialize() { }

        bool IPhysicsService.IsInitialized()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, Hand handedness) { }

        bool IPhysicsService.IsHandEnabled(Hand handedness)
        { return false; }
    }
}