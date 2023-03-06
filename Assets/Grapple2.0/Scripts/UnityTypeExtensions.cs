using UnityEngine;

public static class UnityTypeExtensions
{
    public static bool Approximately(Quaternion q1, Quaternion q2, float acceptableRange)
    {
        return 1 - Mathf.Abs(Quaternion.Dot(q1, q2)) < acceptableRange;
    }

    public static bool Approximately(Vector3 v1, Vector3 v2, float acceptableRange)
    {
        return Approximately(v1.x,v2.x,acceptableRange) && 
               Approximately(v1.y, v2.y, acceptableRange) &&
               Approximately(v1.z, v2.z, acceptableRange);
    }

    public static bool Approximately(float f1, float f2, float acceptableRange)
    {
        return f1 > f2 - acceptableRange && f1 < f2 + acceptableRange;
    }
    
}
