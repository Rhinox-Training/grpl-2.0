using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLSocketable : GRPLBaseInteractable
    {
        public GameObject Socket = null;

        public void OnValidate()
        {
            if (Socket == null)
                PLog.Error<GRPLITLogger>($"{nameof(GRPLSocketable)} Socket was not set.", this);
        }

        public override void Grabbed(GameObject parent, RhinoxHand rhinoxHand)
        {
            base.Grabbed(parent, rhinoxHand);

            Matrix4x4 relativeMatrix;

            //this extra code is only needed for right hand, otherwise the item will be flipped upside down.
            if (rhinoxHand == RhinoxHand.Right)
            {
                //because the right rhinoxHand is mirror of the left one.
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
        }
    }
}