using Rhinox.Perceptor;
using Rhinox.XR.Grapple.It;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple script that is needed for when an object should become a snappable teleport anchor
/// </summary>
/// <remarks>If the script gets reset/added it will supply the transform for the teleport location of the anchor</remarks>
/// <dependencies />
public class GRPLTeleportAnchor : MonoBehaviour
{
    [SerializeField] private Transform _anchorPointTransform = null;
    public Transform AnchorTransform => _anchorPointTransform;

    private void Start()
    {
        if (_anchorPointTransform)
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

    private void OnDrawGizmos()
    {
        if (_anchorPointTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(_anchorPointTransform.position, new Vector3(0.1f, 0.1f, 0.1f));
        }
    }
#endif
}