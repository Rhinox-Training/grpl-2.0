---
layout: page
title: Interactable Bake Settings
parent: Mesh Baking
grand_parent: Grapple IT
nav_order: 3
---

# GRPLInteractableBakeSettings `Public class`

## Description

This class is used to define the bake options for a [GRPLInteractable](./rhinoxxrgrappleit-GRPLInteractable) object.
The [GRPLBakeOptions](./rhinoxxrgrappleit-GRPLBakeOptions) enumeration specifies the different options available.
By setting the BakeOptions field to one of the available options, the behavior of the
interactable object when it is baked can be controlled.

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple.It
  Rhinox.XR.Grapple.It.GRPLInteractableBakeSettings[[GRPLInteractableBakeSettings]]
  end
  subgraph UnityEngine
UnityEngine.MonoBehaviour[[MonoBehaviour]]
  end
UnityEngine.MonoBehaviour --> Rhinox.XR.Grapple.It.GRPLInteractableBakeSettings
```

## Members

### Properties

#### Public  properties

| Type                                                        | Name                                                                                    | Methods |
|-------------------------------------------------------------|-----------------------------------------------------------------------------------------|---------|
| [`GRPLBakeOptions`](./rhinoxxrgrappleit-GRPLBakeOptions) | [`BakeOptions`](#bakeoptions)<br>A getter property that returns the _bakeOptions field. | `get`   |

## Details

### Summary

This class is used to define the bake options for a [GRPLInteractable](./rhinoxxrgrappleit-GRPLInteractable) object.
The [GRPLBakeOptions](./rhinoxxrgrappleit-GRPLBakeOptions) enumeration specifies the different options available.
By setting the BakeOptions field to one of the available options, the behavior of the
interactable object when it is baked can be controlled.

### Inheritance

- `MonoBehaviour`

### Constructors

#### GRPLInteractableBakeSettings

```csharp
public GRPLInteractableBakeSettings()
```

### Properties

#### BakeOptions

```csharp
public GRPLBakeOptions BakeOptions { get; }
```

##### Summary

A getter property that returns the _bakeOptions field.

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
