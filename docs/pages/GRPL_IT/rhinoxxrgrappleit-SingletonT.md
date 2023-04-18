---
layout: page
title: Singleton<T>
parent: Grapple IT
nav_order: 6
---

# Singleton&lt;T&gt; `Public class`

## Diagram

```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph UnityEngine
UnityEngine.MonoBehaviour[[MonoBehaviour]]
UnityEngine.MonoBehaviour[[MonoBehaviour]]
  end
  subgraph Rhinox.XR.Grapple.It
  Rhinox.XR.Grapple.It.Singleton_1[[Singleton< T >]]
  class Rhinox.XR.Grapple.It.Singleton_1 abstractStyle;
  Rhinox.XR.Grapple.It.Singleton_1T((T));
  Rhinox.XR.Grapple.It.Singleton_1 -- where --o Rhinox.XR.Grapple.It.Singleton_1T
UnityEngine.MonoBehaviour --> Rhinox.XR.Grapple.It.Singleton_1T

  end
UnityEngine.MonoBehaviour --> Rhinox.XR.Grapple.It.Singleton_1
```

## Details

### Inheritance

- `MonoBehaviour`

### Constructors

#### Singleton

```csharp
protected Singleton()
```

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)
