using Rhinox.XR.Grapple;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class GestureRecognitionScreen : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _leftScreen;

    [SerializeField]
    private TextMeshProUGUI _rightScreen;

    [SerializeField] private GestureRecognizer _recognizer;

    private void OnValidate()
    {
        Assert.AreNotEqual(null, _leftScreen, $"{nameof(GestureRecognitionScreen)}, left screen not set");
        Assert.AreNotEqual(null, _rightScreen, $"{nameof(GestureRecognitionScreen)}, right screen not set");
        Assert.AreNotEqual(null, _recognizer, $"{nameof(GestureRecognitionScreen)}, gesture recognizer not set");
    }

    private void Start()
    {
        _recognizer.OnGestureRecognized.AddListener(SetScreenText);
        _recognizer.OnGestureUnrecognized.AddListener(RemoveScreenText);
    }

    private void SetScreenText(Hand hand, string gesture)
    {
        switch (hand)
        {
            case Hand.Left:
                _leftScreen.text = gesture;
                break;
            case Hand.Right:
                _rightScreen.text = gesture;
                break;
        }
    }

    private void RemoveScreenText(Hand hand, string gesture)
    {
        switch (hand)
        {
            case Hand.Left:
                if (_leftScreen.text == gesture)
                    _leftScreen.text = "None";
                break;
            case Hand.Right:
                if (_rightScreen.text == gesture)
                    _rightScreen.text = "None";
                break;
        }
    }
}
