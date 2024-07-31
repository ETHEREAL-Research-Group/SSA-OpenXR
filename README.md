# SSA-OpenXR

Welcome to Shared Spatial Anchors for OpenXR, a Unity template repository using Unity 2022, MRTK3, and Azure Spatial Anchros for building multiplayer mixed reality applications. This project focuses on simplifying object sharing in a multi-user environment.

## Getting Started
1. Clone the repository to your local machine.
2. Change the build platform to UWP if you are targeting HoloLens or Android if you are targeting Meta Quest devices.
3. Load the `GameManager` prefab, locate `ASAManager` as a child of `GameManager` and populate the credentials.

## Usage
To share an object:

1. Add the prefabs `NetworkManager`, `GameManager`, and `SharedContent` to your scene.
2. Convert aby game object into a prefab.
3. Attach the `SyncTransform` component to the prefab.
4. Use the `SpawnSharedObject` method from the `GameManager` to spawn the shared object.

### Syncing Transform
The `SyncTransform` component is responsible for synchronizing the transform of shared objects. The owner of the shared object can commit changes to the transform. By default, the owner is set to the host (allowing the host to move the game object). By setting the value of `ChangeOwnershipOnGrab` in `SyncTransform` the app will change the owner of a gameobject when it is grabbed to the user that grabbed it. 

To change ownership programmatically, use the following script:

```csharp
GetComponent<NetworkObject>().ChangeOwnership(ulong clientId);
