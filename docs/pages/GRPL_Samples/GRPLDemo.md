---
layout: default
title: Demo Scene
parent: Grapple Samples
has_children: false
nav_order: 1
---

# Demo scene

![DemoScene_Intro](https://user-images.githubusercontent.com/76707656/233653248-c962f68e-4f8a-4863-ba0f-637513c3decd.PNG)

## Description

This scene is a comprehensive intro to what is possible with the Grapple Interaction Toolkit. It incorporates all the current features:
- Object grabbing and socketing.
- Object pushing.
- Object interaction in the form of buttons, slider or valves.
- Gesture recognition.

There are multiple desks made splitting these features up, to not overwhelm someone with all these features once.

anytime something is grabbed it is using the GRPLGesture system to recognize the grabbing gesture.
**Gesture system**: for grabbing gesture

There is also a teleport in here using gesture system

## Desk 01

Hammer and cup are socketable
the others are just base grabble
button to reset

### using
 - **GRPLInteractable**:  
   - GRPLGrabbableBase and GRPLSocketable for the objects
   - GRPLButton for the reset button
   

![Desk01](https://user-images.githubusercontent.com/76707656/233655362-363a9ab8-5b29-4293-bd3d-1d85556b4933.PNG)

## Desk 02

INFO COMING SOON!

### using

 - **GRPLInteractable**:  
   - GRPLUISlider for the RGB sliders.
   - GRPLValve for the valve to rotate the cube.

![Desk02](https://user-images.githubusercontent.com/76707656/233656970-5ee24e3f-5c77-48af-aea6-d4a3c6d08c7e.PNG)

## Desk 03

INFO COMING SOON!

### using

 - **GRPLInteractable**:  
   - GRPLTwoWayLever, To change from day to night or vice versa.
   - GRPLButton, to select the colors for the Simon says game.

![Desk03](https://user-images.githubusercontent.com/76707656/233655502-14f275a0-c1c3-423a-a3c3-360ea6e02128.PNG)

## Desk 04

INFO COMING SOON!

### using

 - **GRPLGestureSystem**: to recognize the 3 different Rock, Paper and Scissors gesture.
 - **GRPLInteractable**: 
   - GRPLButton, to start or stop the Rock paper scissors game.

![Desk04](https://user-images.githubusercontent.com/76707656/233655457-4bbe3b86-f6fd-4c9a-b9a4-47bcc3e80f1d.PNG)
