using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using Rhinox.XR.Grapple.It;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMaterial : MonoBehaviour
{
    public Material _newWaterial;

    private Material _material;
    private MeshRenderer _meshRenderer;

    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer != null)
        {
            List<Material> tempList = new List<Material>();
            _meshRenderer.GetMaterials(tempList);
            if (tempList.Count > 0)
                _material = tempList[0];
            else
                PLog.Error<GRPLITLogger>("No mat found", this);
        }
        else
            PLog.Error<GRPLITLogger>("No meshrendere found", this);

    }

    public void ChangeMaterToNew()
    {
        _meshRenderer.SetAllMaterials(_newWaterial);
    }

    public void ChangeMatBack()
    {
        _meshRenderer.SetAllMaterials(_material);
    }
}
