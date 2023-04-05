using Rhinox.XR.Grapple.It;
using TMPro;
using UnityEngine;

public class LeverScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dynamicText;
    [SerializeField] private GRPLLever _lever;


    private void Awake()
    {
        OnLeverStopped(null);
    }

    private void OnEnable()
    {
        _lever.LeverActivated += OnLeverActivated;
        _lever.LeverStopped += OnLeverStopped;
    }

    private void OnDisable()
    {
        _lever.LeverActivated -= OnLeverActivated;
        _lever.LeverStopped -= OnLeverStopped;
    }
    
    private void OnLeverStopped(GRPLLever obj)
    {
        _dynamicText.text = "Lever deactivated";
        _dynamicText.color = Color.red;
    }

    private void OnLeverActivated(GRPLLever obj)
    {
        _dynamicText.text = "Lever activated";
        _dynamicText.color = Color.green;

    }
}
