---
layout: page
title: GRPL Logger
parent: Grapple Core
nav_order: 6
---
# GRPLLogger `Internal class`

## Description

This empty class gets used in combination with the Rhinox.Perceptor for custom logging behavior.

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple
  Rhinox.XR.Grapple.GRPLLogger[[GRPLLogger]]
  end
  subgraph Rhinox.Perceptor
Rhinox.Perceptor.CustomLogger[[CustomLogger]]
  end
Rhinox.Perceptor.CustomLogger --> Rhinox.XR.Grapple.GRPLLogger
```

## Details

### Summary

This empty class gets used in combination with the Rhinox.Perceptor for custom logging behavior.

### Inheritance

- `CustomLogger`

### Constructors

#### GRPLLogger

```csharp
public GRPLLogger()
```

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
