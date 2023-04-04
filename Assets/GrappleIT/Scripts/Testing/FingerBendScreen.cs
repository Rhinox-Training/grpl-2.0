using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.XR.Grapple;
using TMPro;
using UnityEngine;

public class FingerBendScreen : MonoBehaviour
{
    [Header("Hand Text fields")]
    [SerializeField] private TextMeshProUGUI _thumb;
    [SerializeField] private TextMeshProUGUI _index;
    [SerializeField] private TextMeshProUGUI _middle;
    [SerializeField] private TextMeshProUGUI _ring;
    [SerializeField] private TextMeshProUGUI _little;

    [Header("Parameters")]
    [SerializeField] private bool _isScaled = true;
    [SerializeField] private RhinoxHand TargetHand = RhinoxHand.Left;

    private GRPLJointManager _jointManager;

    private void Awake()
    {
        GRPLJointManager.GlobalInitialized += OnJointManagerInitialized;
    }

    private void OnEnable()
    {
        if (_jointManager == null)
            return;
        _jointManager.TrackingLost += OnTrackingLost;
        _jointManager.OnHandsUpdated += OnHandsUpdated;
    }

    private void OnDisable()
    {
        if (_jointManager == null)
            return;
        _jointManager.TrackingLost -= OnTrackingLost;
        _jointManager.OnHandsUpdated -= OnHandsUpdated;
    }

    private void OnJointManagerInitialized(GRPLJointManager manager)
    {
        _jointManager = manager;

        // Subscribe tracking functions
        _jointManager.TrackingLost += OnTrackingLost;
        _jointManager.OnHandsUpdated += OnHandsUpdated;
    }

    private void OnTrackingLost(RhinoxHand hand)
    {
        if (hand == RhinoxHand.Invalid)
            return;
        _thumb.text = "0";
        _index.text = "0";
        _middle.text = "0";
        _ring.text = "0";
        _little.text = "0";
    }

    private void OnHandsUpdated(RhinoxHand hand)
    {
        if (hand == RhinoxHand.Invalid || hand != TargetHand)
            return;
        

        float bendValue = 0;
        if (_jointManager.TryGetFingerBend(TargetHand, RhinoxFinger.Thumb, out bendValue, _isScaled))
            _thumb.text = bendValue.ToString("n2");
        if (_jointManager.TryGetFingerBend(TargetHand, RhinoxFinger.Index, out bendValue, _isScaled))
            _index.text = bendValue.ToString("n2");
        if (_jointManager.TryGetFingerBend(TargetHand, RhinoxFinger.Middle, out bendValue, _isScaled))
            _middle.text = bendValue.ToString("n2");
        if (_jointManager.TryGetFingerBend(TargetHand, RhinoxFinger.Ring, out bendValue, _isScaled))
            _ring.text = bendValue.ToString("n2");
        if (_jointManager.TryGetFingerBend(TargetHand, RhinoxFinger.Little, out bendValue, _isScaled))
            _little.text = bendValue.ToString("n2");
    }
}