using Rhinox.Lightspeed;
using Rhinox.XR.Grapple.It;
using System;
using UnityEngine;

public class ColorAndMoveObject : MonoBehaviour
{
    [SerializeField] GRPLUISliderInteractable _sliderR = null;
    [SerializeField] GRPLUISliderInteractable _sliderG = null;
    [SerializeField] GRPLUISliderInteractable _sliderB = null;

    [SerializeField] GRPLValve _valve = null;

    private MeshRenderer _meshrenderer = null;

    // Start is called before the first frame update
    void Start()
    {
        if (!TryGetComponent<MeshRenderer>(out _meshrenderer))
        {
            //_meshrenderer = meshRend.material;
            Debug.LogError("Mesh Renderer was NULL!");
        }

        if (_sliderR == null || _sliderG == null || _sliderB == null || _valve == null)
        {
            Debug.LogError("One of the serialized Fields was NULL!");
            return;
        }

        _sliderR.OnValueUpdate += ColorSliderUpdate;
        _sliderG.OnValueUpdate += ColorSliderUpdate;
        _sliderB.OnValueUpdate += ColorSliderUpdate;

        _valve.OnValueUpdate += ValveUpdate;

        _valve.OnFullyOpen += IsOpen;
        _valve.OnFullyClosed += IsClosed;
    }

    private void IsClosed(GRPLValve valve)
    {
        Debug.LogError("Closed");
    }

    private void IsOpen(GRPLValve valve)
    {
        Debug.LogError("OPEN");
    }

    private void OnDisable()
    {
        _sliderR.OnValueUpdate -= ColorSliderUpdate;
        _valve.OnValueUpdate -= ValveUpdate;
    }

    private void ValveUpdate(GRPLValve valve, ValveState state, float value)
    {
        //rotate around the Y-Axis
        transform.localEulerAngles = transform.localEulerAngles.With(null, value);
    }

    private void ColorSliderUpdate(GRPLUISliderInteractable slider, float value)
    {
        if (slider == _sliderR)
            _meshrenderer.material.color = _meshrenderer.material.color.With(value);
        else if (slider == _sliderG)
            _meshrenderer.material.color = _meshrenderer.material.color.With(null, value);
        else if (slider == _sliderB)
            _meshrenderer.material.color = _meshrenderer.material.color.With(null, null, value);
    }
}
