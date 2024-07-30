using UnityEngine;
using Unity.Netcode;

public class SyncColor : NetworkBehaviour
{
  [SerializeField]
  private string ColorName = "_Base_Color_";
  [SerializeField]
  private MeshRenderer mesh;
  private Color prevColor;
  private bool prevEnabled = false;
  private bool CanWrite = false;

  public override void OnNetworkSpawn()
  {
    base.OnNetworkSpawn();
    if (!mesh)
      mesh = GetComponent<MeshRenderer>();
    prevColor = mesh.material.GetColor(ColorName);
    var temp = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    CanWrite = temp.Role == "Client";
  }

  [ServerRpc(RequireOwnership = false)]
  public void OnColorChange_ServerRpc(Color newColor, ServerRpcParams rpcParams = default)
  {
    OnColorChange_ClientRpc(newColor, rpcParams.Receive.SenderClientId);
  }

  [ClientRpc]
  public void OnColorChange_ClientRpc(Color newColor, ulong senderId)
  {
    // if (!(IsHost || IsInspector)) return;
    if (senderId == NetworkManager.Singleton.LocalClientId) return;
    prevColor = newColor;
    mesh.material.SetColor(ColorName, newColor);
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
    if (senderId == NetworkManager.Singleton.LocalClientId) return;

    prevEnabled = newEnabled;
    mesh.enabled = newEnabled;
  }

  // Update is called once per frame
  void Update()
  {
    if (!IsSpawned || !CanWrite) return;
    var newColor = mesh.material.GetColor(ColorName);
    if (newColor != prevColor)
    {
      prevColor = newColor;
      OnColorChange_ServerRpc(newColor);
    }
    var newEnabled = mesh.enabled;
    if (newEnabled != prevEnabled)
    {
      prevEnabled = newEnabled;
      OnEnabledChange_ServerRpc(newEnabled);
    }
  }
}
