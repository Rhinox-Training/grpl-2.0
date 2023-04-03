using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple script that is needed for when an object should become a snappable teleport anchor
/// </summary>
/// <remarks>If the script gets reset/added it will supply the transform for the teleport location of the anchor</remarks>
/// <dependencies />
public class GRPLTeleportAnchor : MonoBehaviour
{
    public GameObject AnchorTransform => _anchorPointTransform;

    private GameObject _anchorPointTransform = null;

    //-----------------------
    // EDITOR ONLY METHODS
    //-----------------------
#if UNITY_EDITOR
    private void Reset()
    {
        if (_anchorPointTransform == null)
        {
            _anchorPointTransform = new GameObject("TeleportPoint");
            _anchorPointTransform.transform.parent = transform;
            _anchorPointTransform.transform.localPosition = Vector3.zero;
            var iconContent = EditorGUIUtility.IconContent("sv_icon_dot10_pix16_gizmo");
            EditorGUIUtility.SetIconForObject(_anchorPointTransform, (Texture2D)iconContent.image);
        }
    }

    private void OnDrawGizmos()
    {
        if (_anchorPointTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(_anchorPointTransform.transform.position, new Vector3(0.1f, 0.1f, 0.1f));
        }
    }
#endif
}