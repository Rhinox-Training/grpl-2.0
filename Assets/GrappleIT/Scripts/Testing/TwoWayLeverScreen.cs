using Rhinox.XR.Grapple.It;
using TMPro;
using UnityEngine;

public class TwoWayLeverScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dynamicText;
    [SerializeField] private GRPLTwoWayLever _twoWayLever;


    private void Awake()
    {
        OnLeverStopped(null);
    }

    private void OnEnable()
    {
        _twoWayLever.LeverForwardActivated += OnLeverForwardActivated;
        _twoWayLever.LeverBackwardActivated += OnLeverBackwardActivated;
        _twoWayLever.LeverForwardStopped += OnLeverStopped;
        _twoWayLever.LeverBackwardStopped += OnLeverStopped;
    }

    private void OnLeverStopped(GRPLTwoWayLever obj)
    {
        _dynamicText.color = Color.yellow;
        _dynamicText.text = "Neutral";
    }

    private void OnLeverBackwardActivated(GRPLTwoWayLever obj)
    {
        _dynamicText.color = Color.red;
        _dynamicText.text = "Backwards";
    }

    private void OnLeverForwardActivated(GRPLTwoWayLever obj)
    {
        _dynamicText.color = Color.green;
        _dynamicText.text = "Forward";
    }

    private void OnDisable()
    {
        _twoWayLever.LeverForwardActivated += OnLeverForwardActivated;
        _twoWayLever.LeverBackwardActivated += OnLeverBackwardActivated;
        _twoWayLever.LeverForwardStopped += OnLeverStopped;
        _twoWayLever.LeverBackwardStopped += OnLeverStopped;
    }
}