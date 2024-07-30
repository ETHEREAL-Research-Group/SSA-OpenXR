using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Unity.Netcode;

/// <summary>
/// Manages the creation, sending, and location of Azure Spatial Anchors in a Unity scene.
/// </summary>
[RequireComponent(typeof(SpatialAnchorManager))]
[RequireComponent(typeof(NetworkObject))]
public class AnchorManager : NetworkBehaviour
{
  // The parent GameObject of all the game objects that are to be shared.
  [SerializeField]
  private GameObject sharedContent;

  // The prefab used to instantiate GameObjects representing Azure Spatial Anchors.
  [SerializeField]
  private GameObject anchorPrefab;

  // The manager responsible for handling Azure Spatial Anchors.
  private SpatialAnchorManager spatialAnchorManager;

  private GameManager gameManager;

  public override void OnNetworkSpawn()
  {
    base.OnNetworkSpawn();
    gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    // Get reference to the spatial anchor manager in the scene.
    spatialAnchorManager = GetComponent<SpatialAnchorManager>();

    // Define the callback function when an anchor is located.
    spatialAnchorManager.AnchorLocated += OnAnchorLocated;
  }

  void Start()
  {
    // Check if the anchorPrefab is not set and log a warning if not.
    if (anchorPrefab == null)
    {
      Logger.AppendLogError("Anchor prefab is not set. Please assign a value in the Inspector.");
      return;
    }

    
  }

  /// <summary>
  /// Asynchronously creates an Azure Spatial Anchor at the origin and sends it to others.
  /// </summary>
  /// <returns>Task representing the asynchronous operation.</returns>
  public async Task CreateAnchor()
  {
    Logger.AppendLog("CreateAnchor() was called.", false);

    // Start an Azure Spatial Anchors session if not already started.
    if (!spatialAnchorManager.IsSessionStarted)
    {
      try
      {
        await spatialAnchorManager.StartSessionAsync();
        Logger.AppendLog("Session created...", false);

        // Create an anchor at the origin and send it to others.
        await CreateAnchor(new Vector3(0, 0, 0), Quaternion.identity);
        SendAnchor();
      }
      catch (Exception e)
      {
        Logger.AppendLogError($"Failed to start session: {e.Message}");
        throw e;
      }
    }
  }

  private bool UpdateAnchor = false;

  private int Counter = 0;

  public void SendAnchor()
  {
    Logger.AppendLog("SendAnchor() was called.", false);

    // Check if the anchorGameObject is null.
    if (anchorGameObject == null)
    {
      Logger.AppendLogError("Error: anchorGameObject is null.");
      return;
    }

    // Tell clients to delete any spatial anchors that already exist.
    ResetAnchor_ClientRpc();

    // Send the anchor ID to clients.
    LocateAnchor_ClientRpc(anchorId);
    UpdateAnchor = true;
  }

  public void ResetOrigin()
  {
    ResetOrigin_ClientRpc();
  }

  [ClientRpc]
  public void ResetOrigin_ClientRpc()
  {
    sharedContent.transform.SetPositionAndRotation(anchorGameObject.transform.position, anchorGameObject.transform.rotation);
  }

  void FixedUpdate()
  {
    if (!IsHost || !UpdateAnchor) return;
    Counter = (Counter + 1) % (50 * 30);
    if (Counter == 0)
    {
      ReLocateAnchor_ClientRpc();
    }
  }

  // The GameObject representing the current spatial anchor.
  [HideInInspector]
  public GameObject anchorGameObject = null;

  // The unique identifier of the current spatial anchor.
  [HideInInspector]
  public string anchorId = "";

  private bool FirstTime = true;

  /// <summary>
  /// Creates a Unity GameObject to represent a cloud spatial anchor.
  /// </summary>
  /// <param name="args">Event arguments containing information about the located anchor.</param>
  private void CreateAnchor(AnchorLocatedEventArgs args)
  {
    Logger.AppendLog("CreateAnchor(AnchorLocatedEventArgs) called.", false);
    UnityDispatcher.InvokeOnAppThread(() =>
    {
      CloudSpatialAnchor cloudSpatialAnchor = args.Anchor;

      if (!FirstTime || IsHost)
        Destroy(anchorGameObject);

      var tempAnchor = Instantiate(anchorPrefab);

      tempAnchor.AddComponent<CloudNativeAnchor>().CloudToNative(cloudSpatialAnchor);

      anchorGameObject = tempAnchor;

      Logger.AppendLog("Anchor Game Object Created.");
      Logger.AppendLog($"Anchor Pose for Client: {anchorGameObject.transform.position}, {anchorGameObject.transform.rotation}");

      Logger.AppendLog("Changing the origin for the client...");

      sharedContent.transform.SetPositionAndRotation(anchorGameObject.transform.position, anchorGameObject.transform.rotation);

      if (FirstTime) {
        if (!IsHost) {
          Logger.AppendLog("Calling the server...");
          gameManager.FinishSetupAnchor_ServerRpc(OwnerClientId);
        } else {
          RelocatingAnchor = false;  
        }
        FirstTime = false;
      } else {
        RelocatingAnchor = false;
      }
    });
  }

  /// <summary>
  /// Event handler for the anchor located event. Handles different states of anchor location.
  /// </summary>
  /// <param name="sender">The object that triggered the event.</param>
  /// <param name="args">Event arguments containing information about the located anchor.</param>
  private void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
  {
    Logger.AppendLog("OnAnchorLocated(object, AnchorLocatedEventArgs) triggered.", false);

    if (args.Status == LocateAnchorStatus.Located || args.Status == LocateAnchorStatus.AlreadyTracked)
    {
      Logger.AppendLog($"Anchor {(args.Status == LocateAnchorStatus.Located ? "located" : "rediscovered")} successfully. Creating GameObject...", false);
      CreateAnchor(args);
    }
    else
    {
      Logger.AppendLogError($"Anchor location unsuccessful. Status: {args.Status}");
    }
  }

  /// <summary>
  /// Asynchronously creates a cloud spatial anchor based on the provided position and rotation.
  /// </summary>
  /// <param name="position">The position of the anchor in the Unity world.</param>
  /// <param name="rotation">The rotation of the anchor in the Unity world.</param>
  /// <returns>Task representing the asynchronous operation.</returns>
  private async Task CreateAnchor(Vector3 position, Quaternion rotation)
  {
    Logger.AppendLog("CreateAnchor(Vector3, Quaternion) was called.", false);

    var tempAnchor = Instantiate(anchorPrefab, position, rotation);

    // Attach the spatial anchor to the GameObject
    CloudNativeAnchor cloudNativeAnchor = tempAnchor.AddComponent<CloudNativeAnchor>();
    await cloudNativeAnchor.NativeToCloud();
    CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
    cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(10);

    Logger.AppendLog("Capturing spatial data...");

    // Collect spatial data for the anchor
    float prevProgress = .0f;
    while (!spatialAnchorManager.IsReadyForCreate)
    {
      float progress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
      if (progress - prevProgress > 0.1f)
      {
        Logger.AppendLog($"Move your device to capture more environment data: {progress:0%}");
        prevProgress = progress;
      }
    }

    Logger.AppendLog("Spatial data captured. Saving the anchor to the cloud...");

    // Create the anchor in the cloud
    try
    {
      await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);

      // Check if the anchor was saved successfully
      if (cloudSpatialAnchor != null)
      {
        Logger.AppendLog($"Anchor saved to the cloud. ID: {cloudSpatialAnchor.Identifier}");

        anchorGameObject = tempAnchor;
        anchorId = cloudSpatialAnchor.Identifier;
      }
      else
      {
        // Log failure to save anchor
        Logger.AppendLogError("Failed to save anchor to the cloud, but no exception was thrown.");
        return;
      }
    }
    catch (Exception exception)
    {
      // Log any exceptions that occurred during anchor creation
      Logger.AppendLogError("Failed to save anchor to the cloud: " + exception.Message);
    }
  }

  /// <summary>
  /// Asynchronously initiates the location process for previously created anchors.
  /// </summary>
  /// <returns>Task representing the asynchronous operation.</returns>
  public async Task LocateAnchor()
  {
    Logger.AppendLog("LocateAnchor() called.", false);
    Logger.AppendLog($"Anchor ID is: {anchorId}");

    // Ensure the anchor IDs exist.
    if (!string.IsNullOrEmpty(anchorId))
    {
      // Start an Azure Spatial Anchors session if it doesn't exist.
      if (!spatialAnchorManager.IsSessionStarted)
      {
        Logger.AppendLog("spatialAnchorManager.StartSessionAsync() Started");
        await spatialAnchorManager.StartSessionAsync();
        Logger.AppendLog("spatialAnchorManager.StartSessionAsync() successful");
      }

      // Create criteria for anchor location.
      AnchorLocateCriteria anchorLocateCriteria = new()
      {
        Identifiers = new List<string>(1) { anchorId }.ToArray()
      };

      // Check if a spatial anchor watcher already exists.
      if (spatialAnchorManager.Session.GetActiveWatchers().Count > 0)
      {
        Logger.AppendLog("Spatial anchor watcher already exists.", false);
      } else {
        // Create a watcher to locate anchors with the given criteria.
        spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
      }

    }
    else
    {
      // Log a warning if the anchor ID is not available.
      Logger.AppendLogWarning("Anchor ID is not available. Retry scheduled.");
    }
  }

  /// <summary>
  /// Deletes a spatial anchor, including its local reference and associated GameObject.
  /// </summary>
  /// <param name="anchorObj">The GameObject representing the anchor to be deleted.</param>
  public void DeleteAnchor(GameObject anchorObj)
  {
    // Get reference to local spatial anchor.
    CloudNativeAnchor cloudNativeAnchor = anchorObj.GetComponent<CloudNativeAnchor>();
    CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;

    // Log that the DeleteAnchor function was called.
    Logger.AppendLog($"DeleteAnchor() called for anchor with ID: {cloudSpatialAnchor.Identifier}", false);

    // Delete local reference to spatial anchor.
    anchorId = "";

    // Destroy the GameObject representing the anchor.
    Destroy(anchorObj);

    // Log that the anchor has been deleted.
    Logger.AppendLog($"Anchor with ID {cloudSpatialAnchor.Identifier} deleted successfully.", false);
  }

  [ClientRpc]
  public void ResetAnchor_ClientRpc()
  {
    if (IsHost) return;
    Logger.AppendLog("ResetAnchor_ClientRpc() was called.", false);

    // Delete local references to spatial anchors if they exist.
    if (anchorId != "" && anchorGameObject != null)
    {
      DeleteAnchor(anchorGameObject);
    }
  }

  public async Task LocateAnchorWrapper()
  {
    try
    {
      Logger.AppendLog("Locating the anchors...");
      await LocateAnchor();
    }
    catch (Exception e)
    {
      Logger.AppendLogError($"Failed to locate the anchors: {e.Message}, retrying...", true);
      StartCoroutine(SharedFunctions.Retry(LocateAnchorWrapper));
    }
  }

  [ClientRpc]
  public void LocateAnchor_ClientRpc(string id)
  {
    if (IsHost) return;
    Logger.AppendLog("LocateAnchor_ClientRpc() was called", false);

    anchorId = id;
    _ = LocateAnchorWrapper();
  }

  private bool RelocatingAnchor = false;

  [ClientRpc]
  public void ReLocateAnchor_ClientRpc()
  {
    Logger.AppendLog("ReLocateAnchor_ClientRpc() was called", false);
    if (IsHost) {
      sharedContent.transform.SetPositionAndRotation(anchorGameObject.transform.position, anchorGameObject.transform.rotation);
      return;
    }
    if (RelocatingAnchor) {
      Logger.AppendLog("Previouse call to look for anchor is not yet finished");
      return;
    }
    RelocatingAnchor = true;
    _ = LocateAnchorWrapper();
  }
}
