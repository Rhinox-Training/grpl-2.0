---
layout: page
title: Grapple IT Logger
parent: Grapple IT
nav_order: 7
---

# GRPLITLogger `Internal class`

## Description

This empty class gets used in combination with the Rhinox.Perceptor for custom logging behavior.

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple.It
  Rhinox.XR.Grapple.It.GRPLITLogger[[GRPLITLogger]]
  end
  subgraph Rhinox.Perceptor
Rhinox.Perceptor.CustomLogger[[CustomLogger]]
  end
Rhinox.Perceptor.CustomLogger --> Rhinox.XR.Grapple.It.GRPLITLogger
```

## Details

### Summary

This empty class gets used in combination with the Rhinox.Perceptor for custom logging behavior.

### Inheritance

- `CustomLogger`

### Constructors

#### GRPLITLogger

```csharp
public GRPLITLogger()
```

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
