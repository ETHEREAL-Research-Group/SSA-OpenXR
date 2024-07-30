using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour
{
  private AnchorManager anchorManager;
  private GameManager gameManager;

  public override void OnNetworkSpawn()
  {
    Logger.AppendLog($"Object Spawned with Network Object ID = {NetworkObjectId}, Cliend ID = {OwnerClientId} as {(IsHost ? "Host" : "Client")}, your ownership = ${IsOwner}.", false);
    anchorManager = GameObject.FindGameObjectWithTag("AnchorManager").GetComponent<AnchorManager>();
    gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    base.OnNetworkSpawn();
    if (!IsOwner) return;
    if (!IsHost) // Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
    {
      Logger.AppendLog($"You are connected as Client #{OwnerClientId}. Calling the server...");
      Init_ServerRpc(NetworkObjectId, OwnerClientId);
    }
    else // Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
    {
      Logger.AppendLog($"You are now the host.");
    }
  }

  [ServerRpc]
  void Init_ServerRpc(ulong sourceNetworkObjectId, ulong sourceClientId)
  {
    // This is the server
    Logger.AppendLog($"Client #{sourceClientId} is connected.");
    Logger.AppendLog($"Connected client's NetworkObjectID: {sourceNetworkObjectId}", false);
    if (!Application.isEditor)
    {
      if (anchorManager.anchorGameObject != null)
      {
        Logger.AppendLog("Sending the anchors...");
        anchorManager.SendAnchor();
      }
    }
    else
    {
      Logger.AppendLog("Editor mode: Skipping send anchors, finishing setup...");
      gameManager.FinishSetupAnchor_ServerRpc(sourceClientId);
    }
  }
}