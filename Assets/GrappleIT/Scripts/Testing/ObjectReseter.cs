using Rhinox.Lightspeed.Collections;
using Rhinox.XR.Grapple.It;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectReseter : MonoBehaviour
{
    [SerializeField] private GRPLButtonInteractable _resetButton = null;
    public List<GameObject> _objects = null;

    private PairList<Vector3, Quaternion> _transforms = new PairList<Vector3, Quaternion>();
    private bool _init = false;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var obj in _objects)
        {
            _transforms.Add(obj.transform.position, obj.transform.rotation);
        }

        if (_resetButton != null)
            _resetButton.OnInteractStarted += ResetObjects;

        _init = true;
    }

    private void ResetObjects(GRPLInteractable buttonsObj)
    {
        int index = 0;
        foreach (var obj in _objects)
        {
            if (index >= _transforms.Count)
                return;

            obj.transform.SetPositionAndRotation(_transforms[index].V1, _transforms[index].V2);
            index++;
        }
    }

    private void OnEnable()
    {
        if (!_init && _resetButton != null)
            _resetButton.OnInteractStarted += ResetObjects;
    }

    private void OnDisable()
    {
        if (_resetButton != null)
            _resetButton.OnInteractStarted += ResetObjects;
    }
}
