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
        {
            Debug.LogWarning("[RoomChat] More than one RoomChat found in this scene/session.");
        }

        Instance = this;
        Debug.Log($"[RoomChat] Spawned in session '{Runner.SessionInfo?.Name}'.");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
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