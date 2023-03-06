using UnityEngine;

public static class UnityTypeExtensions
{
    public static bool Approximately(Quaternion q1, Quaternion q2, float acceptableRange)
    {
        return 1 - Mathf.Abs(Quaternion.Dot(q1, q2)) < acceptableRange;
    }
}
