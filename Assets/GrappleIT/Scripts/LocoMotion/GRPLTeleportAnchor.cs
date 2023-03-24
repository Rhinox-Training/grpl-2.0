using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GRPLTeleportAnchor : MonoBehaviour
{
    [SerializeField] private GameObject _anchorPointTransform = null;

    public GameObject AnchorTransform => _anchorPointTransform;
}