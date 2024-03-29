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
- Locomotion, teleport.

## Teleportation

To move around, you need to make use of the teleport.  
To [**teleport**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLTeleport.html), make a scissor gesture with either hand into the direction you want to goto.  
After a short delay a arc will show up indicating where you are pointing.  
The demo scene only uses [**teleport anchors**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLTeleportAnchor.html), so you are limited to teleporting between any of the 4 anchors.  
The teleport destination marker will snap to the anchor point if you are close enough.  
To confirm the teleport, you just have to touch the green square with the other hand.

## Desks

There are multiple desks made splitting these features up, to not overwhelm someone with all these features once.
Anytime something is grabbed, it is using the [**Gesture recognition system**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_Core/rhinoxxrgrapple-GRPLGestureRecognizer.html) to recognize the grabbing gesture.

### Desk 1

This desk has small objects you can interact with or pick up.  
The Hammer and cup are socketable objects, meaning they have certain points that will snap to you hands socket point if you try and grab them.
The others objects are just normal grabable objects, meaning that they will keep their orientation when you grab them.
On the right side of the desk is a big red button. this button once pressed, will reset all the objects back to their orriginal position.

#### using
 - [**GRPLProxyPhysics**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_Core/joints.html)
 - [**GRPLInteractable**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/Interactables.html):  
   - [**GRPLGrabbableBase**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLInteractable.html) and
     [**GRPLSocketable**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLSocketable.html) for the objects
   - [**GRPLButton**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLButtonInteractable.html) for the reset button
   

![Desk01](https://user-images.githubusercontent.com/76707656/233655362-363a9ab8-5b29-4293-bd3d-1d85556b4933.PNG)

### Desk 2

This desk has 3 slider on the left side. The sliders control the R,G and B values of the cube in the center respectively. 
You can just touch the slider to change the value, your hand can go through the slider and you can still change the value.  
The interaction only stops when you move your hand back out of the slider box area.
On the right side is a valve, rotating this valve will change Y-axis rotation of the cube.
Just grab the value like you would in real live and turn it. You don't have to worry about moving exactly along the rotational axis.  
There is some leeway. You can either let got or move far away enough from the valve to stop interacting with it.

#### using

 - [**GRPLInteractable**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/Interactables.html):  
   - [**GRPLUISlider**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLUISliderInteractable.html) for the RGB sliders.
   - [**GRPLValve**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLValve.html) for the valve to rotate the cube.

![Desk02](https://user-images.githubusercontent.com/76707656/233656970-5ee24e3f-5c77-48af-aea6-d4a3c6d08c7e.PNG)

### Desk 3

This desk has a lever to change from day to night or night to day.  
Putting the lever in the **UP** position will make it day (if it was night).  
Putting the lever in the **DOWN** position will make it night (if it was day).  
On the right side of the desk is a game of simon says with colors.  
In case you don't know how to play.  
Just press start to begin. Once the game and after a small delay to prepare yourself,
a sequence of colors will be shown. after the sequence is done (starting sequence is 4 long).  
You will need to repeat the sequence back via the small same colored diamond buttons.  
If you fail, all the big diamonds will turn red and your score will return to 0.  
To play again just press the start button.  
If you succeed, you will gain 10 points. the sequence will then play again, but increased by one color.  
This keeps going until you fail.  

#### using

 - [**GRPLInteractable**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/Interactables.html):  
   - [**GRPLTwoWayLever**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLTwoWayLever.html), To change from day to night or vice versa.
   - [**GRPLButton**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLButtonInteractable.html), to select the colors for the Simon says game.

![Desk03](https://user-images.githubusercontent.com/76707656/233655502-14f275a0-c1c3-423a-a3c3-360ea6e02128.PNG)

### Desk 4

This desk has just a simple game of Rock paper scissors, based on the gesture your hand makes.
Press the start button to start. Once started a hand will appear in front of you making one of the 3 gestures at random.
After 3 seconds it will stop and use that gesture as its move.  
You will need to make either a rock, paper or scissors gesture before the PC is done making it's choice.
The gesture you made as soon as the PC is done will be used to calculate the scores.
The winner gets 3 points and loser nothing, if it was a draw, you both get 1 point.
This cycle keeps going until you press the stop button.

**NOTE**:  
The teleport is deactivated during the rock paper scissors game.  
As soon as the game has been stopped you will be able to teleport again. 

#### using

 - [**Gesture recognition system**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_Core/rhinoxxrgrapple-GRPLGestureRecognizer.html): to recognize the 3 different Rock, Paper and Scissors gesture.
 - [**GRPLInteractable**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/Interactables.html):  
   - [**GRPLButton**](https://rhinox-training.github.io/grpl-2.0/pages/GRPL_IT/rhinoxxrgrappleit-GRPLButtonInteractable.html), to start or stop the Rock paper scissors game.

![Desk04](https://user-images.githubusercontent.com/76707656/233655457-4bbe3b86-f6fd-4c9a-b9a4-47bcc3e80f1d.PNG)
