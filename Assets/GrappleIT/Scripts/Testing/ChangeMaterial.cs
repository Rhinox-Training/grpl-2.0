using Rhinox.Lightspeed;
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
                Debug.LogError("No mat found");
        }
        else
            Debug.LogError("no meshrendere");

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
