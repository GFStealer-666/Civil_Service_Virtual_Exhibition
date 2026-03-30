using System;
using Fusion;
using UnityEngine;

public class RoomChat : NetworkBehaviour
{
    public static RoomChat Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private int maxMessageLength = 100;
    [SerializeField] private float sendCooldownSeconds = 0.4f;

    public event Action<string, string> OnMessageReceived;

    private float _lastSendTime;

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[RoomChat] More than one RoomChat found in this scene/session.");

        Instance = this;
        Debug.Log($"[RoomChat] Spawned on {gameObject.name}, active={gameObject.activeInHierarchy}, scene={gameObject.scene.name}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log($"[RoomChat] Despawned on {gameObject.name}, scene={gameObject.scene.name}");

        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        Debug.Log($"[RoomChat] OnDisable on {gameObject.name}");
    }

    private void OnDestroy()
    {
        Debug.Log($"[RoomChat] OnDestroy on {gameObject.name}");
    }
    public bool TrySendMessage(string senderName, string message)
    {
        if (string.IsNullOrWhiteSpace(senderName))
            senderName = "Guest";

        if (string.IsNullOrWhiteSpace(message))
            return false;

        message = message.Trim();

        if (message.Length > maxMessageLength)
            message = message.Substring(0, maxMessageLength);

        if (Time.unscaledTime - _lastSendTime < sendCooldownSeconds)
            return false;

        _lastSendTime = Time.unscaledTime;

        RPC_BroadcastMessage(senderName, message);
        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_BroadcastMessage(string senderName, string message, RpcInfo info = default)
    {
        OnMessageReceived?.Invoke(senderName, message);
        Debug.Log($"[RoomChat] {senderName}: {message}");
    }
}