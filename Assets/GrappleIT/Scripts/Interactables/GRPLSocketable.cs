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

        public override void Grabbed(GameObject parent, Hand hand)
        {
            base.Grabbed(parent, hand);

            Matrix4x4 relativeMatrix;

            if (hand == Hand.Right)
            {
                //because the right hand is mirror of the left one.
                //the object needs to be flip around the X-axis to mirror it. 
                Socket.transform.localRotation = new Quaternion(Socket.transform.localRotation.x * -1.0f,
                                            Socket.transform.localRotation.y,
                                            Socket.transform.localRotation.z,
                                            Socket.transform.localRotation.w * -1.0f);

                relativeMatrix = Socket.transform.GetMatrixRelativeTo(this.GetWorldMatrix());

                //is a component, so i can't take a copy of it, meaning i have to reset the orriginal back.
                Socket.transform.localRotation = new Quaternion(Socket.transform.localRotation.x * -1.0f,
                                            Socket.transform.localRotation.y,
                                            Socket.transform.localRotation.z,
                                            Socket.transform.localRotation.w * -1.0f);
            }
            else
            {
                relativeMatrix = Socket.transform.GetMatrixRelativeTo(this.GetWorldMatrix());
            }

            var relativeInverse = relativeMatrix.inverse;
            var targetMatrix = parent.GetWorldMatrix() * relativeInverse;

            transform.SetPositionAndRotation(Utility.GetMatrixPosition(targetMatrix), Utility.GetMatrixRotation(targetMatrix));




            //var relativeInverse = relativeMatrix.inverse;
            //var targetMatrix = parent.GetWorldMatrix() * relativeInverse;

            //transform.SetPositionAndRotation(Utility.GetMatrixPosition(targetMatrix), Utility.GetMatrixRotation(targetMatrix));


            //if (hand == Hand.Right)
            //{
            //    transform.localRotation = new Quaternion(transform.localRotation.x * -1.0f,
            //                                transform.localRotation.y,
            //                                transform.localRotation.z,
            //                                transform.localRotation.w * -1.0f);
            //}
        }
    }
}