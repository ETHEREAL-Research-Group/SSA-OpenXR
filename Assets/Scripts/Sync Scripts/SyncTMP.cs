using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class SyncTMP : NetworkBehaviour
{
  private TMP_Text tmp;
  private string prevText = "";

  // Start is called before the first frame update
  public override void OnNetworkSpawn()
  {
    tmp = GetComponent<TextMeshPro>();
    // prevText = tmp.text;
  }

  [ServerRpc(RequireOwnership = false)]
  private void OnTextChange_ServerRpc(string newText, ServerRpcParams rpcParams = default)
  {
    OnTextChange_ClientRpc(newText, rpcParams.Receive.SenderClientId);
  }

  [ClientRpc]
  private void OnTextChange_ClientRpc(string newText, ulong senderId)
  {
    if (senderId == NetworkManager.Singleton.LocalClientId) return;
    prevText = newText;
    tmp.text = newText;
  }

  // Update is called once per frame
  void Update()
  {
    if (!IsSpawned) return;
    if (tmp.text != prevText)
    {
      prevText = tmp.text;
      OnTextChange_ServerRpc(tmp.text);
    }
  }
}
