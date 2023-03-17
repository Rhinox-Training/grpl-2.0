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
        bool GetInitialised();
        void Update();
        void SetHandEnabled(bool newState, Hand handedness);
        bool GetIsHandEnabled(Hand handedness);
    }

    public class NullPhysicsService : IPhysicsService
    {
        void IPhysicsService.TryInitialize() { }

        bool IPhysicsService.GetInitialised()
        { return false; }

        public void Update() { }

        void IPhysicsService.SetHandEnabled(bool newState, Hand handedness) { }

        bool IPhysicsService.GetIsHandEnabled(Hand handedness)
        { return false; }
    }
}