---
title: Home
layout: home
nav_order: 1
---

# Grapple 2.0
Welcome to the Grapple 2.0 documentation!
Grapple is an open source, hand tracking framework for Unity. It is designed to create hand tracking VR applications that can be easily ported to different platforms. Grapple consists of the **Grapple Core** and **Grapple Interaction Toolkit**.

The github repository can be found [here](https://github.com/Rhinox-Training/grpl-2.0)

## Grapple Core
Within the Core package, the core fundamentals of a hand tracking application and some utilities are implemented.
These are:
- Joint system
- Gesture system
- Hand visualization
- Extension methods for Unity types

To have a look at the documentation of Grapple Core, [click here](pages/GRPL_Core/core)

## Grapple Interaction Toolkit
The Grapple Interaction Toolkit provides functionality to create interactive experiences in Unity.
The main features of the Grapple IT package are: 
- An interactable system, with a base class to inherit from and multiple implementations.
- Teleport functionality, to traverse the world with.
- Mesh baking functionality.
- 3D math functions.
- Gizmo extension to generate 3D shapes in the scene view.

To have a look at the documentation of Grapple Interaction Toolkit, [click here](pages/GRPL_IT/GrappleIT)


## Grapple Samples
Within the Grapple sample package are 2 scenes.

- GRPLDemo, this scene uses all the system available in the Grapple Interaction Toolkit to showcase how they can be used.
- GRPLGestureTester, this scene gives you the finger bending values and current gesture each hand is making. This is really useful for testing and tweaking new or existing gestures.

To have a look at a more in depth explenation about the Grapple Sample scenes, [click here](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_Samples/GRPLSamples.html)

#License

Apache-2.0 © Rhinox NV
