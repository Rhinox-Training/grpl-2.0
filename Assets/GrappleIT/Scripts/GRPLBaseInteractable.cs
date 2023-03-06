using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public abstract class GRPLBaseInteractable
    {
        public abstract void Grabbed(GameObject parent);

        public abstract void Dropped();
    }
}
