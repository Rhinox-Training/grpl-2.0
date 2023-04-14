# GRPLGestureRecognizer `Public class`

## Description
This class implements the behaviour to detect gestures. These gestures can be imported from a json or recording during play mode.
            There is also the possibility to export the gestures in a (new) json file.

## Diagram
```mermaid
  flowchart LR
  classDef interfaceStyle stroke-dasharray: 5 5;
  classDef abstractStyle stroke-width:4px
  subgraph Rhinox.XR.Grapple
  Rhinox.XR.Grapple.GRPLGestureRecognizer[[GRPLGestureRecognizer]]
  end
  subgraph UnityEngine
UnityEngine.MonoBehaviour[[MonoBehaviour]]
  end
UnityEngine.MonoBehaviour --> Rhinox.XR.Grapple.GRPLGestureRecognizer
```

## Members
### Properties
#### Public  properties
| Type | Name | Methods |
| --- | --- | --- |
| [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | [`CurrentLeftGesture`](#currentleftgesture)<br>A RhinoxGesture object that represents the current gesture of the left hand. | `get` |
| [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | [`CurrentRightGesture`](#currentrightgesture)<br>A RhinoxGesture object that represents the current gesture of the right hand. | `get` |
| `bool` | [`LeftHandGestureRecognizedThisFrame`](#lefthandgesturerecognizedthisframe)<br>A boolean flag that indicates whether a gesture was recognized for the first time this frame on the left hand. | `get, private set` |
| `bool` | [`RightHandGestureRecognizedThisFrame`](#righthandgesturerecognizedthisframe)<br>A boolean flag that indicates whether a gesture was recognized for the first time this frame on the right hand. | `get, private set` |

### Methods
#### Private  methods
| Returns | Name |
| --- | --- |
| `bool` | [`<SaveGesture>b__49_0`](#savegestureb490)([`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) x) |
| `void` | [`Awake`](#awake)() |
| `void` | [`HandleRecognizedGesture`](#handlerecognizedgesture)([`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) newGesture, ref [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) currentGesture, ref [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) lastGesture, [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) handedness) |
| `void` | [`Initialize`](#initialize)([`GRPLJointManager`](./rhinoxxrgrapple-GRPLJointManager) jointManager)<br>As the bone manager is an integral part of gesture recognition, this should always be called when creating this component! |
| `void` | [`InvokeGestureLostEvents`](#invokegesturelostevents)(ref [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) currentGesture, ref [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) lastGesture, [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) rhinoxHand) |
| `void` | [`OnDestroy`](#ondestroy)() |
| `void` | [`OnDisable`](#ondisable)() |
| `void` | [`OnEnable`](#onenable)() |
| `void` | [`OnTrackingLost`](#ontrackinglost)([`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) rhinoxHand) |
| `void` | [`ReadGesturesFromJson`](#readgesturesfromjson-12)()<br>Reds the gestures from the json file at "ImportFilePath". See [GRPLGestureRecognizer](rhinoxxrgrapple-GRPLGestureRecognizer).ReadGesturesFromJson(System.String) |
| `void` | [`RecognizeGesture`](#recognizegesture)([`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) handedness)<br>Checks if the given rhinoxHand "handedness" is currently representing a gesture. If a gesture is recognized, it is set as the current gesture and the corresponding events are invoked. Use the "RecognitionDistanceThreshold" and "RecognitionForwardThreshold" to change the harshness of the recognition. |
| `void` | [`SaveGesture`](#savegesture)(`CallbackContext` ctx) |
| `void` | [`Update`](#update)() |

#### Public  methods
| Returns | Name |
| --- | --- |
| [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | [`GetCurrentGestureOfHand`](#getcurrentgestureofhand)([`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) hand)<br>Returns the current gesture on the given hand if the hand is either left or right. <br>            Returns null if the given hand is invalid. |
| `void` | [`ReadGesturesFromJson`](#readgesturesfromjson-22)(`string` path)<br>Imports the gestures from the given json file at path "path". If the directory or file is not valid, an empty list is added. Specify whether to overwrite the current gesture using "OverwriteGesturesOnImport" |
| `bool` | [`WasRecognizedGestureStartedThisFrame`](#wasrecognizedgesturestartedthisframe)([`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) hand)<br>Returns whether the current gesture on the given hand was recognized for the first time this frame. <br>            Returns false if the given hand is invalid. |
| `void` | [`WriteGesturesToJson`](#writegesturestojson)()<br>Writes all current gestures to a .json file at directory "ExportFilePath" with name "ExportFileName".json. If the ExportFilePath directory is not valid, the application data path is used. |

## Details
### Summary
This class implements the behaviour to detect gestures. These gestures can be imported from a json or recording during play mode.
            There is also the possibility to export the gestures in a (new) json file.

### Inheritance
 - `MonoBehaviour`

### Constructors
#### GRPLGestureRecognizer
```csharp
public GRPLGestureRecognizer()
```

### Fields
#### OverwriteGesturesOnImport
```csharp
public  OverwriteGesturesOnImport
```
##### Summary
A boolean flag to indicate if imported gestures should overwrite any existing gestures or not.

#### ImportOnPlay
```csharp
public  ImportOnPlay
```
##### Summary
A boolean flag to indicate if gestures should be imported when the play mode starts.

#### ImportFilePath
```csharp
public  ImportFilePath
```
##### Summary
A string that represents the path to the file containing gestures.

#### ExportOnDestroy
```csharp
public  ExportOnDestroy
```
##### Summary
A boolean flag to indicate if gestures should be exported when the component is destroyed.

#### ExportFilePath
```csharp
public  ExportFilePath
```
##### Summary
A string that represents the path where the exported gestures should be saved.

#### ExportFileName
```csharp
public  ExportFileName
```
##### Summary
A string that represents the name of the exported gestures file.

#### RecordActionReference
```csharp
public  RecordActionReference
```
##### Summary
An InputActionReference object that represents the input action that should be used to record new gestures.

#### SavedGestureName
```csharp
public  SavedGestureName
```
##### Summary
A string that represents the name of the next gesture that is recorded.

#### HandToRecord
```csharp
public  HandToRecord
```
##### Summary
An enum value that represents the hand that should be used to record new gestures.

#### UseJointForward
```csharp
public  UseJointForward
```
##### Summary
A boolean flag that indicates whether the forward vector of a joint should be used when trying to recognize the next recorded gesture.

#### ForwardJoint
```csharp
public  ForwardJoint
```
##### Summary
An enum value that represents the joint that should be used as the forward vector.

#### GestureBendThreshold
```csharp
public  GestureBendThreshold
```
##### Summary
A float value that represents the bend threshold used to compare the bend values of gestures to the current hand.

#### GestureForwardThreshold
```csharp
public  GestureForwardThreshold
```
##### Summary
A float value that represents the angle threshold used to compare the direction of a gesture with the forward vector of a joint.

#### Gestures
```csharp
public  Gestures
```
##### Summary
A list of RhinoxGesture objects that represents the gestures that can be recognized by the component.

#### GlobalInitialized
```csharp
private static  GlobalInitialized
```

#### OnGestureRecognized
```csharp
public  OnGestureRecognized
```
##### Summary
A Unity event that is invoked when any gesture is recognized.

#### OnGestureUnrecognized
```csharp
public  OnGestureUnrecognized
```
##### Summary
A Unity event that is invoked when any gesture is unrecognized.

#### _currentLeftGesture
```csharp
private  _currentLeftGesture
```

#### _currentRightGesture
```csharp
private  _currentRightGesture
```

#### _lastLeftGesture
```csharp
private  _lastLeftGesture
```
##### Summary
A RhinoxGesture object that represents the gesture on the left hand in the previous frame.

#### _lastRightGesture
```csharp
private  _lastRightGesture
```
##### Summary
A RhinoxGesture object that represents the gesture on the right hand in the previous frame.

#### _jointManager
```csharp
private  _jointManager
```
##### Summary
A GRPLJointManager object that is used to track the hand joints.

#### _isInitialized
```csharp
private  _isInitialized
```
##### Summary
A boolean flag that indicates whether the component has been initialized.

### Methods
#### Initialize
```csharp
private void Initialize(GRPLJointManager jointManager)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`GRPLJointManager`](./rhinoxxrgrapple-GRPLJointManager) | jointManager |  |

##### Summary
As the bone manager is an integral part of gesture recognition, this should always be called when creating this component!

#### Awake
```csharp
private void Awake()
```

#### OnTrackingLost
```csharp
private void OnTrackingLost(RhinoxHand rhinoxHand)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) | rhinoxHand |   |

#### InvokeGestureLostEvents
```csharp
private void InvokeGestureLostEvents(ref RhinoxGesture currentGesture, ref RhinoxGesture lastGesture, RhinoxHand rhinoxHand)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| `ref` [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | currentGesture |   |
| `ref` [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | lastGesture |   |
| [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) | rhinoxHand |   |

#### OnDestroy
```csharp
private void OnDestroy()
```

#### OnEnable
```csharp
private void OnEnable()
```

#### OnDisable
```csharp
private void OnDisable()
```

#### Update
```csharp
private void Update()
```

#### RecognizeGesture
```csharp
private void RecognizeGesture(RhinoxHand handedness)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) | handedness |  |

##### Summary
Checks if the given rhinoxHand "handedness" is currently representing a gesture. If a gesture is recognized, it is set as the current gesture and the corresponding events are invoked. Use the "RecognitionDistanceThreshold" and "RecognitionForwardThreshold" to change the harshness of the recognition.

#### HandleRecognizedGesture
```csharp
private void HandleRecognizedGesture(RhinoxGesture newGesture, ref RhinoxGesture currentGesture, ref RhinoxGesture lastGesture, RhinoxHand handedness)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | newGesture |   |
| `ref` [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | currentGesture |   |
| `ref` [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | lastGesture |   |
| [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) | handedness |   |

#### GetCurrentGestureOfHand
```csharp
public RhinoxGesture GetCurrentGestureOfHand(RhinoxHand hand)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) | hand |  |

##### Summary
Returns the current gesture on the given hand if the hand is either left or right. 
            Returns null if the given hand is invalid.

##### Returns
The gesture on hand.

#### WasRecognizedGestureStartedThisFrame
```csharp
public bool WasRecognizedGestureStartedThisFrame(RhinoxHand hand)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`RhinoxHand`](./rhinoxxrgrapple-RhinoxHand) | hand |  |

##### Summary
Returns whether the current gesture on the given hand was recognized for the first time this frame. 
            Returns false if the given hand is invalid.

##### Returns
Whether the current gesture on the given hand was recognized for the first time this frame.

#### SaveGesture
```csharp
private void SaveGesture(CallbackContext ctx)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| `CallbackContext` | ctx |   |

#### WriteGesturesToJson
```csharp
public void WriteGesturesToJson()
```
##### Summary
Writes all current gestures to a .json file at directory "ExportFilePath" with name "ExportFileName".json. If the ExportFilePath directory is not valid, the application data path is used.

#### ReadGesturesFromJson [1/2]
```csharp
private void ReadGesturesFromJson()
```
##### Summary
Reds the gestures from the json file at "ImportFilePath". See [GRPLGestureRecognizer](rhinoxxrgrapple-GRPLGestureRecognizer).ReadGesturesFromJson(System.String)

#### ReadGesturesFromJson [2/2]
```csharp
public void ReadGesturesFromJson(string path)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| `string` | path |  |

##### Summary
Imports the gestures from the given json file at path "path". If the directory or file is not valid, an empty list is added. Specify whether to overwrite the current gesture using "OverwriteGesturesOnImport"

#### <SaveGesture>b__49_0
```csharp
private bool <SaveGesture>b__49_0(RhinoxGesture x)
```
##### Arguments
| Type | Name | Description |
| --- | --- | --- |
| [`RhinoxGesture`](./rhinoxxrgrapple-RhinoxGesture) | x |   |

### Properties
#### CurrentLeftGesture
```csharp
public RhinoxGesture CurrentLeftGesture { get; }
```
##### Summary
A RhinoxGesture object that represents the current gesture of the left hand.

#### CurrentRightGesture
```csharp
public RhinoxGesture CurrentRightGesture { get; }
```
##### Summary
A RhinoxGesture object that represents the current gesture of the right hand.

#### LeftHandGestureRecognizedThisFrame
```csharp
public bool LeftHandGestureRecognizedThisFrame { get; private set; }
```
##### Summary
A boolean flag that indicates whether a gesture was recognized for the first time this frame on the left hand.

#### RightHandGestureRecognizedThisFrame
```csharp
public bool RightHandGestureRecognizedThisFrame { get; private set; }
```
##### Summary
A boolean flag that indicates whether a gesture was recognized for the first time this frame on the right hand.

### Events
#### GlobalInitialized
```csharp
public static event Action<GRPLGestureRecognizer> GlobalInitialized
```

*Generated with* [*ModularDoc*](https://github.com/hailstorm75/ModularDoc)