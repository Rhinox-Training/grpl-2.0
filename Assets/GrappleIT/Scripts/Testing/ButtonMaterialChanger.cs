using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;

public class ButtonMaterialChanger : MonoBehaviour
{
   [SerializeField] private GRPLButtonInteractable _interactable;
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
      if(_interactable == null)return;
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
      Assert.AreNotEqual(_target,null,"[ButtonMaterialChanger,OnValidate], _target not set!");
      Assert.AreNotEqual(_defaultMaterial, null, "[ButtonMaterialChanger,OnValidate], _defaultMaterial not set!");
      Assert.AreNotEqual(_proximityMaterial, null, "[ButtonMaterialChanger,OnValidate], _proximityMaterial not set!");
      Assert.AreNotEqual(_interactedMaterial, null, "[ButtonMaterialChanger,OnValidate], _interactedMaterial not set!");
   }

   private void OnProximityStarted()
   {
      _target.material = _proximityMaterial;
   }

   private void OnProximityEnded()
   {
      _target.material = _defaultMaterial;
   }

   private void OnInteractedStarted()
   {
      _target.material = _interactedMaterial;
   }

   private void OnInteractedEnded()
   {
      _target.material = _defaultMaterial;
   }

}
