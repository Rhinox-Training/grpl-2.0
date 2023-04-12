using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.XR.Grapple;
using Rhinox.XR.Grapple.It;
using UnityEngine;

public class BakeAtParentTest : MonoBehaviour
{
   [SerializeField] private MeshBaker _meshBaker;
   [SerializeField] private Transform _parent;

   private void Update()
   {
      if (Input.GetKeyDown(KeyCode.B))
      {
         _meshBaker.BakeMeshAndParentToTransform(RhinoxHand.Left, _parent);
      }
      if(Input.GetKeyDown(KeyCode.N))
      {
         _meshBaker.DestroyBakedObjects(RhinoxHand.Left);
      }
   }
}
