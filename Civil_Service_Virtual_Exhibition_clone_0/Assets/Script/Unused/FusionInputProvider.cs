using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class FusionInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private readonly Dictionary<PlayerRef, LocalPlayerInput> _sources = new();

    public void SetLocalInputSource(PlayerRef player, LocalPlayerInput input)
    {
        if (input == null) return;
        _sources[player] = input;
        Debug.Log($"[FusionInputProvider] Bound {player} -> {input.gameObject.name}");
    }

    // Call this from your NetworkManager/GameLauncher after runner starts
    public void RegisterWithRunner(NetworkRunner runner)
    {
        _runner = runner;
        runner.AddCallbacks(this);
        Debug.Log("[FusionInputProvider] Registered with runner");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var localPlayer = runner.LocalPlayer;

        if (!_sources.TryGetValue(localPlayer, out var src) || src == null)
        {
            Debug.LogWarning($"[FusionInputProvider] No source for {localPlayer}");
            return;
        }

        var data = new NetworkInputData
        {
            Move   = src.Move,
            Look   = src.Look,
            Jump   = src.Jump,
            Sprint = src.Sprint
        };

        Debug.Log($"[FusionInputProvider] Input Move={data.Move} Look={data.Look}");
        input.Set(data);
        src.ConsumeOneShotInputs();
    }

#region INetworkRunnerCallbacks
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnPlayerJoined(NetworkRunner r, PlayerRef p) { }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason reason) { }
    public void OnConnectedToServer() { }
    public void OnDisconnectedFromServer() { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress addr, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessage msg) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> list) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float progress) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }
#endregion
}
public struct NetworkInputData : INetworkInput
{
    public Vector2 Move;
    public Vector2 Look;
    public NetworkBool Jump;
    public NetworkBool Sprint;
}