using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This class is used to define the bake options for a <see cref="GRPLInteractable"/> object.
    /// The <See cref="GRPLBakeOptions"/> enumeration specifies the different options available.
    /// By setting the BakeOptions field to one of the available options, the behavior of the
    /// interactable object when it is baked can be controlled.
    /// </summary>
    public class GRPLInteractableBakeSettings : MonoBehaviour
    {
        /// <summary>
        /// This <see cref="GRPLBakeOptions"/> field stores the desired bake options for the <see cref="GRPLInteractable"/> object.
        /// </summary>
        [SerializeField] private GRPLBakeOptions _bakeOptions = GRPLBakeOptions.NoBake;
        /// <summary>
        /// A getter property that returns the _bakeOptions field.
        /// </summary>
        public GRPLBakeOptions BakeOptions => _bakeOptions;
    }
}