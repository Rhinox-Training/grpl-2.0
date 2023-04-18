---
layout: page
title: Grapple Interactable Manager
parent: Interactables
grand_parent: Grapple IT
nav_order: 4
---

# GRPLInteractableManager `Public class`

## Description

This object is responsible for calculating all interactions between the hands defined by the jointManager
field and all interactables in the scene that inherit from GRPLInteractable.

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple.It
  Rhinox.XR.Grapple.It.GRPLInteractableManager[[GRPLInteractableManager]]
  Rhinox.XR.Grapple.It.Singleton_1[[Singleton< T >]]
  class Rhinox.XR.Grapple.It.Singleton_1 abstractStyle;
  Rhinox.XR.Grapple.It.Singleton_1T((T));
  Rhinox.XR.Grapple.It.Singleton_1 -- where --o Rhinox.XR.Grapple.It.Singleton_1T
UnityEngine.MonoBehaviour --> Rhinox.XR.Grapple.It.Singleton_1T

  end
  subgraph UnityEngine
UnityEngine.MonoBehaviour[[MonoBehaviour]]
  end
Rhinox.XR.Grapple.It.Singleton_1 --> Rhinox.XR.Grapple.It.GRPLInteractableManager
```

## Members

### Methods

#### Public  methods

| Returns | Name                                                                         |
|---------|------------------------------------------------------------------------------|
| `void`  | [`Awake`](#awake)()<br>Initializes this instance of GRPLInteractableManager. |

## Details

### Summary

This object is responsible for calculating all interactions between the hands defined by the jointManager
field and all interactables in the scene that inherit from GRPLInteractable.

### Inheritance

- [`Singleton`](./rhinoxxrgrappleit-SingletonT)
  &lt;[`GRPLInteractableManager`](rhinoxxrgrappleit-GRPLInteractableManager)&gt;

### Constructors

#### GRPLInteractableManager

```csharp
public GRPLInteractableManager()
```

### Methods

#### Awake

```csharp
public void Awake()
```

##### Summary

Initializes this instance of GRPLInteractableManager.

### Events

#### InteractibleInteractionCheckPaused

```csharp
public event Action<RhinoxHand, GRPLInteractable> InteractibleInteractionCheckPaused
```

##### Summary

Invoked when an interactable's interaction check is paused.

#### InteractibleInteractionCheckResumed

```csharp
public event Action<RhinoxHand, GRPLInteractable> InteractibleInteractionCheckResumed
```

##### Summary

Invoked when an interactable's interaction check is resumed.

#### InteractibleLeftProximity

```csharp
public event Action<RhinoxHand, GRPLInteractable> InteractibleLeftProximity
```

##### Summary

Invoked when an interactable is no longer in proximity of a hand.

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
