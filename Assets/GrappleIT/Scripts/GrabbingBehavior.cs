using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbingBehavior : MonoBehaviour
{
    public enum GrabbingBehaviorType
    {
        NotGrabbable,
        Grabbable,
        Sockatable
    }

    public GrabbingBehaviorType GrabbingType = GrabbingBehaviorType.Grabbable;
}
