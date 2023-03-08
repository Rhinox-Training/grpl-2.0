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


        public override void OnValidate()
        {
            base.OnValidate();

            //Assert.AreEqual(Socket, null,);
            if (Socket == null)
                Debug.LogError($"{nameof(GRPLSocketable)} Socket was not set.");
            //Assert.IsNull(Socket, $"{nameof(GRPLSocketable)} Socket was not set.");
        }

        public override void Grabbed(GameObject parent, Vector3 positionalOffset, Quaternion rotationalOffset)
        {
            //Vector3 vectorDistance = AbsVectorBetween(parent.transform.position, Socket.transform.position);


            base.Grabbed(parent, positionalOffset, rotationalOffset);
            //transform.position += (parent.transform.position - Socket.transform.position);// + new Vector3(0, -0.035f, 0.035f);

            //transform.position -= (parent.transform.position - Socket.transform.position);// + new Vector3(0, -0.0167f, 0.0332f);

            var relativeMatrix = this.Socket.transform.GetMatrixRelativeTo(this.GetWorldMatrix());
            var relativeInverse = relativeMatrix.inverse;
            var targetMatrix = parent.GetWorldMatrix() * relativeInverse;

            transform.SetPositionAndRotation(Utility.GetMatrixPosition(targetMatrix), Utility.GetMatrixRotation(targetMatrix));

            //transform.localPosition = Vector3.zero + (transform.position - Socket.transform.position);
            //transform.localRotation = Quaternion.identity;// * Socket.transform.localRotation;

            //var angleDiff = parent.transform.rotation * Quaternion.Inverse(Socket.transform.rotation);
            //transform.rotation = angleDiff * transform.rotation;

            //transform.rotation = Quaternion.Euler(15f, 0, 15f) * transform.rotation;


        }

        private Vector3 AbsVectorBetween(Vector3 a, Vector3 b)
        {
            Vector3 temp = a - b;
            return new Vector3(Mathf.Abs(temp.x), Mathf.Abs(temp.y), Mathf.Abs(temp.z)); ;
        }
    }
}