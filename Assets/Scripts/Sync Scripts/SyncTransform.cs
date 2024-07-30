using MixedReality.Toolkit.SpatialManipulation;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
/// for transforms that'll always be owned by the server.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class SyncTransform : NetworkTransform
{
  public bool ChangeOwnershipOnGrab = false;
  public override void OnNetworkSpawn()
  {
    Logger.AppendLog("setting the parent", false);
    var sharedContent = GameObject.FindGameObjectWithTag("Content").transform;
    GetComponent<NetworkObject>().TrySetParent(sharedContent, false);
    base.OnNetworkSpawn();
    Logger.AppendLog("ChangeOwnershipOnGrab is " + ChangeOwnershipOnGrab, false);
    if (ChangeOwnershipOnGrab)
    {
      var component = GetComponent<ObjectManipulator>();
      if (!component) component = GetComponentInChildren<ObjectManipulator>();
      component.IsGrabSelected.OnEntered.AddListener((float _) => OnGrabStarted());
    }
  }

  // Constructor
  public SyncTransform()
  {
    // Set desired default values for properties
    InLocalSpace = true;
    Interpolate = true;
    ChangeOwnershipOnGrab = false;
  }

  public void OnGrabStarted()
  {
    if (IsOwner)
    {
      Logger.AppendLog("This player is already the owner", false);
      return;
    }
    if (IsServer)
    {
      // change ownership
      Logger.AppendLog("server is changing the ownership to be owned by server", false);
      GetComponent<NetworkObject>().ChangeOwnership(0);
      return;
    }
    // This is the client
    // call the server and ask for changing the ownership
    Logger.AppendLog("calling the server to change the ownership", false);
    ChangeOwnership_ServerRpc();
  }

  [ServerRpc(RequireOwnership = false)]
  private void ChangeOwnership_ServerRpc(ServerRpcParams serverRpcParams = default)
  {
    // Call this RPC to request ownership change from the server
    // The server will handle the ownership change
    Logger.AppendLog($"server is changing the ownership to client {serverRpcParams.Receive.SenderClientId}", false);
    GetComponent<NetworkObject>().ChangeOwnership(serverRpcParams.Receive.SenderClientId);
  }

  /// <summary>
  /// Used to determine who can write to this transform. Owner client only.
  /// This imposes state to the server. This is putting trust on your clients. Make sure no security-sensitive features use this transform.
  /// </summary>
  protected override bool OnIsServerAuthoritative()
  {
    return false;
  }
}