using Rhinox.XR.Grapple.It;
using TMPro;
using UnityEngine;

public class LeverScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dynamicText;
    [SerializeField] private GRPLOneWayLever _oneWayLever;


    private void Awake()
    {
        OnOneWayLeverStopped(null);
    }

    private void OnEnable()
    {
        _oneWayLever.LeverActivated += OnOneWayLeverActivated;
        _oneWayLever.LeverStopped += OnOneWayLeverStopped;
    }

    private void OnDisable()
    {
        _oneWayLever.LeverActivated -= OnOneWayLeverActivated;
        _oneWayLever.LeverStopped -= OnOneWayLeverStopped;
    }
    
    private void OnOneWayLeverStopped(GRPLOneWayLever obj)
    {
        _dynamicText.text = "Lever deactivated";
        _dynamicText.color = Color.red;
    }

    private void OnOneWayLeverActivated(GRPLOneWayLever obj)
    {
        _dynamicText.text = "Lever activated";
        _dynamicText.color = Color.green;

    }
}
