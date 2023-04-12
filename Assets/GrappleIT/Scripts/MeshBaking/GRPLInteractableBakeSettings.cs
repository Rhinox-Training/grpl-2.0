using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLInteractableBakeSettings : MonoBehaviour
    {
        [SerializeField] private GRPLBakeOptions _bakeOptions = GRPLBakeOptions.NoBake;
        public GRPLBakeOptions BakeOptions => _bakeOptions;
    }
}