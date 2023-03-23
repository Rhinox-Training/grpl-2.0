using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ButtonMaterialChanger : MonoBehaviour
{
   [SerializeField] private MeshRenderer _target;
   [SerializeField] private Material _defaultMaterial;
   [SerializeField] private Material _proximityMaterial;
   [SerializeField] private Material _interactedMaterial;

   private void Awake()
   {
      _target.material = _defaultMaterial;
   }

   private void OnValidate()
   {
      Assert.AreNotEqual(_target,null,"[ButtonMaterialChanger,OnValidate], _target not set!");
      Assert.AreNotEqual(_defaultMaterial, null, "[ButtonMaterialChanger,OnValidate], _defaultMaterial not set!");
      Assert.AreNotEqual(_proximityMaterial, null, "[ButtonMaterialChanger,OnValidate], _proximityMaterial not set!");
      Assert.AreNotEqual(_interactedMaterial, null, "[ButtonMaterialChanger,OnValidate], _interactedMaterial not set!");
   }

   public void OnProximityStarted()
   {
      _target.material = _proximityMaterial;
   }

   public void OnProximityEnded()
   {
      _target.material = _defaultMaterial;
   }

   public void OnInteractedStarted()
   {
      _target.material = _interactedMaterial;
   }

   public void OnInteractedEnded()
   {
      _target.material = _defaultMaterial;
   }

}
