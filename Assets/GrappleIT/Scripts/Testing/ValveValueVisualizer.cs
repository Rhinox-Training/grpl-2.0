using Rhinox.XR.Grapple.It;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ValveValueVisualizer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _screen;
    [SerializeField] private GRPLValve _valve;

    private void OnEnable()
    {
        //if (_screen != null)
        //    _screen.text = (_sliderInteractable.SliderValue * 100f).ToString("0.00") + "%";

        _valve.OnValueUpdate += ValveUpdate;
    }

    private void OnDisable()
    {
        _valve.OnValueUpdate -= ValveUpdate;
    }

    private void ValveUpdate(GRPLValve _, ValveState __, float value)
    {
        if (_screen != null)
        {
            _screen.text = value.ToString("0.00") + " deg";
        }
    }
}
