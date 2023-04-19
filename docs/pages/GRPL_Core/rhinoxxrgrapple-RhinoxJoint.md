---
layout: page
title: Rhinox Joint
parent: Joint System
grand_parent: Grapple Core
nav_order: 2
---
# RhinoxJoint `Public class`

## Description

Represents a joint in a Grapple application.

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple
  Rhinox.XR.Grapple.RhinoxJoint[[RhinoxJoint]]
  end
```

## Details

### Summary

Represents a joint in a Grapple application.

### Constructors

#### RhinoxJoint

```csharp
public RhinoxJoint(XRHandJointID jointID)
```

##### Arguments

| Type            | Name    | Description                                                 |
|-----------------|---------|-------------------------------------------------------------|
| `XRHandJointID` | jointID | The ID of the XRHandJoint that this RhinoxJoint represents. |

##### Summary

Creates a new RhinoxJoint instance for the specified XRHandJointID.

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
