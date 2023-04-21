using Rhinox.Perceptor;
using Rhinox.XR.Grapple.It;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This class is a simple script that is used to make an object become a snappable teleport anchor.
/// When this script is added to an object, it will supply the transform for the teleport location of the anchor.
/// </summary>
/// <remarks>If the script gets reset/added again, it will automatically create an anchor point transform for the object.</remarks>
/// <dependencies />
public class GRPLTeleportAnchor : MonoBehaviour
{
    /// <summary>
    /// Holds the transform of the anchor point.
    /// </summary>
    [SerializeField] private Transform _anchorPointTransform = null;

    /// <summary>
    /// Returns the transform of the anchor point.
    /// </summary>
    public Transform AnchorTransform => _anchorPointTransform;

    /// <summary>
    /// Checks whether the anchor point transform is null and logs a warning message if it is.
    /// </summary>
    private void Start()
    {
        if (_anchorPointTransform == null)
            PLog.Warn<GRPLITLogger>($"[GRPLTeleportAnchor:Start], Anchor Point Transform was NULL!", this);
    }

    //-----------------------
    // EDITOR ONLY METHODS
    //-----------------------
#if UNITY_EDITOR
    private void Reset()
    {
        if (_anchorPointTransform == null)
        {
            var anchorGo = new GameObject("TeleportPoint");
            _anchorPointTransform = anchorGo.transform;
            _anchorPointTransform.parent = transform;
            _anchorPointTransform.localPosition = Vector3.zero;
            var iconContent = EditorGUIUtility.IconContent("sv_icon_dot10_pix16_gizmo");
            EditorGUIUtility.SetIconForObject(_anchorPointTransform.gameObject, (Texture2D)iconContent.image);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_anchorPointTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(_anchorPointTransform.position, new Vector3(0.1f, 0.1f, 0.1f));
        }
    }
#endif
}