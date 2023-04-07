using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;

public class InteractibleMaterialChanger : MonoBehaviour
{
    [SerializeField] private GRPLInteractable _interactable;
    [SerializeField] private MeshRenderer _target;
    [SerializeField] private Material _defaultMaterial;
    [SerializeField] private Material _proximityMaterial;
    [SerializeField] private Material _interactedMaterial;

    private void Awake()
    {
        _target.material = _defaultMaterial;
    }

    private void OnEnable()
    {
        if (_interactable == null) return;
        OnDisable();
        _interactable.OnInteractStarted += OnInteractedStarted;
        _interactable.OnInteractEnded += OnInteractedEnded;
        _interactable.OnProximityStarted += OnProximityStarted;
        _interactable.OnProximityEnded += OnProximityEnded;
    }

    private void OnDisable()
    {
        if (_interactable == null) return;
        _interactable.OnInteractStarted -= OnInteractedStarted;
        _interactable.OnInteractEnded -= OnInteractedEnded;
        _interactable.OnProximityStarted -= OnProximityStarted;
        _interactable.OnProximityEnded -= OnProximityEnded;
    }


    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable, null, "[ButtonMaterialChanger,OnValidate], _interactable not set!");
        Assert.AreNotEqual(_target, null, "[ButtonMaterialChanger,OnValidate], _target not set!");
    }

    private void OnProximityStarted(GRPLInteractable obj)
    {
        if (_proximityMaterial)
            _target.material = _proximityMaterial;
    }

    private void OnProximityEnded(GRPLInteractable obj)
    {
        if (_defaultMaterial)
            _target.material = _defaultMaterial;
    }

    private void OnInteractedStarted(GRPLInteractable obj)
    {
        if (_interactedMaterial)
            _target.material = _interactedMaterial;
    }

    private void OnInteractedEnded(GRPLInteractable obj)
    {
        if (_defaultMaterial)
            _target.material = _defaultMaterial;
    }
}