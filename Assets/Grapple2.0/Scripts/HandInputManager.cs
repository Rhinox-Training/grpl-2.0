using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;


namespace Rhinox.XR.Grapple
{
    public class HandInputManager : MonoBehaviour
    {
        private BoneManager _boneManager = null;

        private IPhysicsService _physicsService = null;

        private GestureRecognizer _gestureRecognizer = null;
        
        
        void Start()
        {
            _boneManager = gameObject.AddComponent<BoneManager>();
            if (_boneManager == null)
            {
                Debug.LogError($"{nameof(HandInputManager)} Failed to add {nameof(BoneManager)}");
            }

            _gestureRecognizer =  gameObject.AddComponent<GestureRecognizer>();
            _gestureRecognizer.Initialize(_boneManager);
        }
    }
}
