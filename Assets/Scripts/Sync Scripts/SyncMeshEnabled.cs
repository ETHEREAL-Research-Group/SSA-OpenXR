using Unity.Netcode;
using UnityEngine;

public class SyncMeshEnabled : NetworkBehaviour
{
  [SerializeField]
  private MeshRenderer mesh;
  private bool prevEnabled = true;
  private bool CanWrite = false;

  public override void OnNetworkSpawn()
  {
    base.OnNetworkSpawn();
    if (!mesh)
      mesh = GetComponent<MeshRenderer>();
    var temp = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    CanWrite = temp.Role == "Client";
  }
  [ServerRpc(RequireOwnership = false)]
  public void OnEnabledChange_ServerRpc(bool newEnabled, ServerRpcParams rpcParams = default)
  {
    OnEnabledChange_ClientRpc(newEnabled, rpcParams.Receive.SenderClientId);
  }

  [ClientRpc]
  public void OnEnabledChange_ClientRpc(bool newEnabled, ulong senderId)
  {
    // if (!(IsHost || IsInspector)) return;
    // if (senderId == NetworkManager.Singleton.LocalClientId) return;
    if (CanWrite) return;

    prevEnabled = newEnabled;
    mesh.enabled = newEnabled;
  }

  // Update is called once per frame
  void Update()
  {
    if (!IsSpawned || !CanWrite) return;
    var newEnabled = mesh.enabled;
    if (newEnabled != prevEnabled)
    {
      prevEnabled = newEnabled;
      OnEnabledChange_ServerRpc(newEnabled);
    }
  }
}
