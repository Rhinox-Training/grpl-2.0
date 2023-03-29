using Rhinox.XR.Grapple;
using Rhinox.XR.Grapple.It;
using UnityEngine;

public class MeshBakeTest : MonoBehaviour
{
    [SerializeField] private MeshBaker _meshBaker;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _meshBaker.BakeMesh(RhinoxHand.Left);
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            _meshBaker.DestroyBakedObjects(RhinoxHand.Left);
        }
    }
}
