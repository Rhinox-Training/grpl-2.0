using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.XR.Grapple.It;
using TMPro;
using UnityEngine;

public class NumberScreen : MonoBehaviour
{
    [SerializeField] private List<GRPLInteractable> _numberButtons;
    [SerializeField] private GRPLInteractable _resetButton;
    [SerializeField] private GRPLInteractable _backSpaceButton;
    [SerializeField] private TextMeshProUGUI _screen;

    private void Awake()
    {
        _screen.text = "";
    }

    private void OnEnable()
    {
        _resetButton.OnInteractStarted -= OnReset;
        _resetButton.OnInteractStarted += OnReset;

        _backSpaceButton.OnInteractStarted -= OnBackSpace;
        _backSpaceButton.OnInteractStarted += OnBackSpace;

        foreach (var button in _numberButtons)
        {
            button.OnInteractStarted -= OnButtonPressed;
            button.OnInteractStarted += OnButtonPressed;
        }
    }

    private void OnDisable()
    {
        _resetButton.OnInteractStarted -= OnReset;
        _backSpaceButton.OnInteractStarted -= OnBackSpace;

        foreach (var button in _numberButtons)
            button.OnInteractStarted -= OnButtonPressed;
    }

    private void OnReset(GRPLInteractable interactable)
    {
        _screen.text = "";
    }

    private void OnBackSpace(GRPLInteractable interactable)
    {
        if (_screen.text.Length > 0)
            _screen.text = _screen.text.Remove(_screen.text.Length - 1);
    }

    private void OnButtonPressed(GRPLInteractable interactable)
    {
        _screen.text += interactable.gameObject.name;
    }
}