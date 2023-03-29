using Rhinox.XR.Grapple.It;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public class PercentageVisualizer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _screen;
    [SerializeField] private GRPLSliderInteractable _sliderInteractable;

    private void OnEnable()
    {
        //if (_screen != null)
        //    _screen.text = (_sliderInteractable.SliderValue * 100f).ToString("0.00") + "%";

        _sliderInteractable.OnValueUpdate += SliderUpdate;
    }

    private void OnDisable()
    {
        _sliderInteractable.OnValueUpdate -= SliderUpdate;
    }

    private void SliderUpdate(float value)
    {
        if (_screen != null)
        {
            _screen.text = (value * 100f).ToString("0.00") + "%";
            _screen.color = Color.Lerp(Color.red, Color.green, value);
        }
    }
}