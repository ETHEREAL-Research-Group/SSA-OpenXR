# SSA-OpenXR

Welcome to Shared Spatial Anchors for OpenXR, a Unity template repository using Unity 2022, MRTK3, and Azure Spatial Anchros for building multiplayer mixed reality applications. This project focuses on simplifying object sharing in a multi-user environment.

## Getting Started
1. Clone the repository to your local machine.
2. Change the build platform to UWP if you are targeting HoloLens or Android if you are targeting Meta Quest devices.

## Usage
To share an object:

1. Add the prefabs `NetworkManager`, `GameManager`, and `SharedContent` to your scene.
2. Convert aby game object into a prefab.
2. Attach the `SyncTransform` component to the prefab.
3. Use the `SpawnSharedObject` method from the `GameManager` to spawn the shared object.

### Syncing Transform
The `SyncTransform` component is responsible for synchronizing the transform of shared objects. The owner of the shared object can commit changes to the transform. By default, the owner is set to the host (allowing the host to move the game object).

To change ownership programmatically, use the following script:

```csharp
GetComponent<NetworkObject>().ChangeOwnership(ulong clientId);
