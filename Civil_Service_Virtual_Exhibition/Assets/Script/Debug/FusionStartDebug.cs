using System;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class FusionStartDebug : MonoBehaviour, INetworkRunnerCallbacks {
  public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {
    Debug.LogError($"[FUSION] OnShutdown reason={shutdownReason} state={runner.State} mode={runner.Mode} gameMode={runner.GameMode}");
  }
  public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
    Debug.LogError($"[FUSION] OnConnectFailed {reason} addr={remoteAddress}");
  }
  public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {
    Debug.LogError($"[FUSION] OnDisconnected {reason}");
  }

  // unused callbacks (required by interface)
  public void OnConnectedToServer(NetworkRunner runner)
  {
    Debug.Log($"[FUSION] OnConnectedToServer state={runner.State} mode={runner.Mode} gameMode={runner.GameMode}");
  }
  public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {}
  public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {}
  public void OnInput(NetworkRunner runner, NetworkInput input) {}
  public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}
  public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) {}
  public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) {}
  public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
  {
    Debug.Log($"[FUSION] OnHostMigration state={runner.State} mode={runner.Mode} gameMode={runner.GameMode}");
  }
  public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) {}
  public void OnSceneLoadDone(NetworkRunner runner) {}
  public void OnSceneLoadStart(NetworkRunner runner) {}
  public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }
}