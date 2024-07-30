using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(LobbyManager))]
public class GameManager : NetworkBehaviour
{
  [SerializeField]
  private GameObject SharedContent;
  public GameObject SpawnedObj = null;

  [ServerRpc(RequireOwnership = false)]
  public void FinishSetupAnchor_ServerRpc(ulong sourceClientId)
  {
    Logger.AppendLog($"Setup Anchor is finished for client #{sourceClientId}");
    if (sourceClientId == 2)
    {
      Logger.AppendLog($"Researcher is Connected. Finishing Setup...");
      FinishSetup_ServerRpc();
    }
  }

  private string UserID;

  [ServerRpc(RequireOwnership = false)]
  public void FinishSetup_ServerRpc()
  {
    Logger.AppendLog("Setup is finished. Creating User ID...");
    UserID = Guid.NewGuid().ToString()[..6];
    Logger.AppendLog($"UserID = {UserID}");
    FinishSetup_ClientRpc(UserID);
  }

  [HideInInspector]
  public string Role;

  [ClientRpc]
  public void FinishSetup_ClientRpc(string _UserID)
  {
    if (!IsHost)
      UserID = _UserID;
    if (!Application.isEditor)
    {
      // anchorManager.anchorGameObject.SetActive(false);
    }
    Logger.StartTracking($"{UserID}-{Role}");
    if (Role == "Client")
    {
      GameObject.Find("Logger").GetComponent<Logger>().HideLogger();
    }
  }


  public GameObject SpwanSharedObject(GameObject prefab, ulong ownerId = 0)  // 0 is the host
  {
    Logger.AppendLog($"Creating the shared gameobject: {prefab.name}");
    GameObject go;
    if (SpawnedObj != null)
    {
      SpawnedObj.transform.GetPositionAndRotation(out var _position, out var _rotation);
      Destroy(SpawnedObj);
      go = Instantiate(prefab, _position, _rotation);
    }
    else
      go = Instantiate(prefab);
    go.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
    SpawnedObj = go;
    return go;
  }

  private async Task CreateAnchorWrapper()
  {
    try
    {
      Logger.AppendLog("Creating the anchors...");
      await anchorManager.CreateAnchor();
    }
    catch (Exception e)
    {
      Logger.AppendLogError($"Failed to create the anchors: {e.Message}, retrying...", true);
      StartCoroutine(SharedFunctions.Retry(CreateAnchorWrapper));
    }
  }

  public async void StartHost()
  {
    try
    {
      Logger.AppendLog("Creating the Lobby...");
      roles.SetActive(false);
      await lobbyManager.CreateLobby();
      Role = "Host";
      if (!Application.isEditor)
        _ = CreateAnchorWrapper();
      else
        Logger.AppendLog("ASA cannot be tested withing the editor.");
    }
    catch (Exception e)
    {
      Logger.AppendLogError($"Failed to start the host: {e.Message}", true);
      roles.SetActive(true);
    }
  }

  public async void StartClient()
  {
    try
    {
      Logger.AppendLog("Joining the Lobby...");
      roles.SetActive(false);
      await lobbyManager.QuickJoinLobby();
      Role = "Client";
    }
    catch (Exception e)
    {
      Logger.AppendLogError($"Failed to start the client: {e.Message}. retrying...", true);
      roles.SetActive(true);
    }
  }

  private LobbyManager lobbyManager;
  private AnchorManager anchorManager;
  [SerializeField]
  private GameObject roles;

  // Start is called before the first frame update
  void Start()
  {
    lobbyManager = GetComponent<LobbyManager>();
    anchorManager = GameObject.FindGameObjectWithTag("AnchorManager").GetComponent<AnchorManager>();
  }

  public override void OnNetworkDespawn()
  {
    Logger.StopTracking();
  }
}
