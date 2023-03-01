using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Rhinox.XR.Grapple
{
    public class HandInputManager : MonoBehaviour
    {
        private BoneManager _boneManager = null;

        private IPhysicsService _physicsService = null;

        void Start()
        {
            _boneManager = gameObject.AddComponent<BoneManager>();
            if (_boneManager == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failled to add {nameof(BoneManager)}");
            }


        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
