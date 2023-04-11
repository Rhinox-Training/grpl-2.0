using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveMovement : MonoBehaviour
{
    public float Amplitude;
    public float Frequency;

    private float angle;
    private void Update()
    {
        angle += Time.deltaTime * Frequency;
        transform.localPosition = new Vector3(0,Mathf.Sin(angle) * Amplitude, 0);
    }
}
