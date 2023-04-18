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

        _valve.ValueUpdated += SliderUpdate;
    }

    private void OnDisable()
    {
        _valve.ValueUpdated -= SliderUpdate;
    }

    private void SliderUpdate(GRPLValve obj, float value)
    {
        if (_screen != null)
        {
            _screen.text = value.ToString("0.00") + " deg";
            //_screen.color = Color.Lerp(Color.red, Color.green, value);
        }
    }
}
