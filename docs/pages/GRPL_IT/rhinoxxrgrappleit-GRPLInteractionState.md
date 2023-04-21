---
layout: page
title: Grapple Interaction State
parent: Interactables
grand_parent: Grapple IT
nav_order: 2
---

# GRPLInteractionState `Public enum`

## Description

The possible states a Grapple Interactible can be in.

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple.It
  Rhinox.XR.Grapple.It.GRPLInteractionState[[GRPLInteractionState]]
  end
```

## Details

### Summary

The possible states a Grapple Interactible can be in.

### Fields

#### Active

##### Summary

The neutral state of a grapple interactible. This means all checks can happen for the object.

#### Proximate

##### Summary

This state is used when a grapple interactible is in proximity to hands.

#### Interacted

##### Summary

This state is used when a hand is currently interacting with the grapple interactible.

#### Disabled

##### Summary

This state is used when a grapple interactible is disabled and no proximity or interactions checks should happen.

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
