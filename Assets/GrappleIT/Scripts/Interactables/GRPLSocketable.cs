using log4net.Util;
using Rhinox.Lightspeed;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLSocketable : GRPLBaseInteractable
    {
        public GameObject Socket = null;

        public void OnValidate()
        {
            //base.OnValidate();

            if (Socket == null)
                Debug.LogError($"{nameof(GRPLSocketable)} Socket was not set.");
        }

        public override void Grabbed(GameObject parent)
        {
            base.Grabbed(parent);

            var relativeMatrix = this.Socket.transform.GetMatrixRelativeTo(this.GetWorldMatrix());
            var relativeInverse = relativeMatrix.inverse;
            var targetMatrix = parent.GetWorldMatrix() * relativeInverse;

            transform.SetPositionAndRotation(Utility.GetMatrixPosition(targetMatrix), Utility.GetMatrixRotation(targetMatrix));
        }
    }
}