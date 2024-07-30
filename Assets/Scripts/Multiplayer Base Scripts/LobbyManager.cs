using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

#if UNITY_EDITOR
using ParrelSync;
#endif

/// <summary>
/// Manages the creation and maintenance of multiplayer lobbies using Unity Netcode, Unity Services, and Relay.
/// Handles player authentication, lobby initialization, quick joining existing lobbies, and provides cleanup procedures.
/// </summary>
public class LobbyManager : MonoBehaviour
{
  private Lobby _connectedLobby;
  private UnityTransport _transport;
  private const string JoinCodeKey = "j";
  private string _playerId;

  private async void Awake()
  {
    _transport = FindObjectOfType<UnityTransport>();
    await Authenticate();
  }

  /// <summary>
  /// Authenticates the player anonymously.
  /// </summary>
  private async Task Authenticate()
  {
    var options = new InitializationOptions();

#if UNITY_EDITOR
    // Remove this if you don't have ParrelSync installed. 
    // It's used to differentiate the clients; otherwise, the lobby will count them as the same
    options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
#endif

    try
    {
      // Initialize Unity Services
      await UnityServices.InitializeAsync(options);

      // Sign in anonymously
      await AuthenticationService.Instance.SignInAnonymouslyAsync();
      _playerId = AuthenticationService.Instance.PlayerId;

      // Log successful authentication
      Logger.AppendLog($"Authentication successful. PlayerId: {_playerId}", false);
    }
    catch (Exception e)
    {
      // Log authentication error
      Logger.AppendLogError($"Authentication failed: {e.Message}", true);
      throw;
    }
  }

  /// <summary>
  /// Attempts to quickly join an existing lobby.
  /// </summary>
  public async Task QuickJoinLobby()
  {
    try
    {
      // Authenticate the player
      // await Authenticate();

      // Attempt to join a lobby in progress
      var lobby = await Lobbies.Instance.QuickJoinLobbyAsync();

      // If a lobby is found, grab the relay allocation details
      var allocation = await RelayService.Instance.JoinAllocationAsync(lobby.Data[JoinCodeKey].Value);

      // Set the details to the transport for client connection
      SetTransformAsClient(allocation);

      // Join the game room as a client
      NetworkManager.Singleton.StartClient();
      _connectedLobby = lobby;

      // Log successful lobby join
      Logger.AppendLog($"Joined lobby: {_connectedLobby.Id}", false);
    }
    catch (Exception e)
    {
      // Log error when quick join fails
      Logger.AppendLogError($"Quick join failed: {e.Message}", true);
      _connectedLobby = null;
      throw;
    }
  }

  /// <summary>
  /// Creates a new lobby.
  /// </summary>
  public async Task CreateLobby()
  {
    try
    {
      // Authenticate the player
      // await Authenticate();

      const int maxPlayers = 100;

      // Create a relay allocation and generate a join code to share with the lobby
      var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
      var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

      // Create a lobby, adding the relay join code to the lobby data
      var options = new CreateLobbyOptions
      {
        Data = new Dictionary<string, DataObject> { { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
      };
      var lobby = await Lobbies.Instance.CreateLobbyAsync("Useless Lobby Name", maxPlayers, options);

      // Send a heartbeat every 15 seconds to keep the room alive
      StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

      // Set the game room to use the relay allocation
      _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

      // Start the room immediately; you might want to wait for the lobby to fill up
      NetworkManager.Singleton.StartHost();
      _connectedLobby = lobby;

      // Log successful lobby creation
      Logger.AppendLog($"Created lobby: {_connectedLobby.Id}", false);
    }
    catch (Exception e)
    {
      // Log error when lobby creation fails
      Logger.AppendLogError($"Failed to create a lobby: {e.Message}", true);
      _connectedLobby = null;
      throw;
    }
  }

  /// <summary>
  /// Sets the transport details for connecting as a client.
  /// </summary>
  private void SetTransformAsClient(JoinAllocation allocation)
  {
    // Ensure _transport is not null
    if (_transport != null)
    {
      _transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);
    }
    else
    {
      // Log a warning if _transport is null
      Logger.AppendLogWarning("UnityTransport not found. Client relay data not set.");
    }
  }

  /// <summary>
  /// Sends heartbeat pings to keep the lobby alive.
  /// </summary>
  private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
  {
    var delay = new WaitForSecondsRealtime(waitTimeSeconds);
    while (true)
    {
      // Send heartbeat ping to the lobby
      Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
      yield return delay;
    }
  }

  private void OnDestroy()
  {
    try
    {
      StopAllCoroutines();

      // Check if the player is the host and then perform cleanup
      if (_connectedLobby != null)
      {
        if (_connectedLobby.HostId == _playerId)
        {
          // Delete the lobby if the player is the host
          Lobbies.Instance.DeleteLobbyAsync(_connectedLobby.Id);
          Logger.AppendLog($"Lobby {_connectedLobby.Id} deleted by host.", false);
        }
        else
        {
          // Remove the player if the player is not the host
          Lobbies.Instance.RemovePlayerAsync(_connectedLobby.Id, _playerId);
          Logger.AppendLog($"Player {_playerId} removed from lobby {_connectedLobby.Id}.", false);
        }
      }
    }
    catch (Exception e)
    {
      // Log error during cleanup
      Logger.AppendLogError($"Error shutting down lobby: {e.Message}");
    }
  }
}
